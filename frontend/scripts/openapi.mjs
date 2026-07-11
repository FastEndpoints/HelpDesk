import { readFile, writeFile, mkdir, mkdtemp, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import process from 'node:process';
import openapiTS, { astToString, COMMENT_HEADER } from 'openapi-typescript';

const root = resolve(import.meta.dirname, '..');
const services = {
	identity: process.env.IDENTITY_OPENAPI_URL ?? 'http://localhost:5000/openapi/v1.json',
	profile: process.env.PROFILE_OPENAPI_URL ?? 'http://localhost:5001/openapi/v1.json'
};

function sortObject(value) {
	if (Array.isArray(value)) return value.map(sortObject);
	if (value && typeof value === 'object') {
		return Object.fromEntries(
			Object.keys(value)
				.sort()
				.map((key) => [key, sortObject(value[key])])
		);
	}
	return value;
}

async function normalize(document) {
	return `${JSON.stringify(sortObject(document), null, 2)}\n`;
}

async function generate(specPath) {
	const ast = await openapiTS(pathToFileURL(specPath));
	return `${COMMENT_HEADER}${astToString(ast)}`;
}

async function fetchSpec(url) {
	const response = await fetch(url, { signal: AbortSignal.timeout(15_000) });
	if (!response.ok) throw new Error(`OpenAPI fetch failed: ${response.status} ${url}`);
	return normalize(await response.json());
}

async function generateInto(specDirectory, outputDirectory) {
	await mkdir(outputDirectory, { recursive: true });
	for (const name of Object.keys(services)) {
		await writeFile(
			join(outputDirectory, `${name}.d.ts`),
			await generate(join(specDirectory, `${name}.json`))
		);
	}
}

async function assertSame(expectedPath, actualPath, label) {
	const [expected, actual] = await Promise.all([
		readFile(expectedPath, 'utf8'),
		readFile(actualPath, 'utf8')
	]);
	if (expected !== actual)
		throw new Error(`${label} is stale; run pnpm api:refresh && pnpm api:generate`);
}

const command = process.argv[2];
const specDirectory = join(root, 'openapi');
const generatedDirectory = join(root, 'src/lib/api/generated');

if (command === 'refresh') {
	const specs = await Promise.all(
		Object.entries(services).map(async ([name, url]) => [name, await fetchSpec(url)])
	);
	for (const [name, contents] of specs)
		await writeFile(join(specDirectory, `${name}.json`), contents);
} else if (command === 'generate') {
	await generateInto(specDirectory, generatedDirectory);
} else if (command === 'check' || command === 'check-live') {
	const temporary = await mkdtemp(join(tmpdir(), 'helpdesk-openapi-'));
	try {
		const temporarySpecs = join(temporary, 'openapi');
		const temporaryGenerated = join(temporary, 'generated');
		await mkdir(temporarySpecs);
		for (const [name, url] of Object.entries(services)) {
			const contents =
				command === 'check-live'
					? await fetchSpec(url)
					: await readFile(join(specDirectory, `${name}.json`), 'utf8');
			await writeFile(join(temporarySpecs, `${name}.json`), contents);
			if (command === 'check-live')
				await assertSame(
					join(specDirectory, `${name}.json`),
					join(temporarySpecs, `${name}.json`),
					`${name} OpenAPI snapshot`
				);
		}
		await generateInto(temporarySpecs, temporaryGenerated);
		for (const name of Object.keys(services))
			await assertSame(
				join(generatedDirectory, `${name}.d.ts`),
				join(temporaryGenerated, `${name}.d.ts`),
				`${name} generated types`
			);
	} finally {
		await rm(temporary, { recursive: true, force: true });
	}
} else {
	throw new Error('Usage: openapi.mjs refresh|generate|check|check-live');
}
