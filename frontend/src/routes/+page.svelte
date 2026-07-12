<script lang="ts">
	import { resolve } from '$app/paths';
</script>

<svelte:head>
	<title>HelpDesk</title>
	<meta
		name="description"
		content="HelpDesk user identity and profile services, delivered through a secure server-side frontend."
	/>
</svelte:head>

<main class="px-6 py-20">
	<section class="mx-auto max-w-4xl">
		<p class="mb-4 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">HelpDesk</p>
		<h1 class="max-w-3xl text-5xl font-bold tracking-tight text-fe-heading sm:text-7xl">
			A secure home for your support profile.
		</h1>
		<p class="mt-8 max-w-2xl text-lg leading-8 text-fe-text-muted">
			The SvelteKit frontend connects to the brokerless .NET service mesh through a server-only API
			boundary.
		</p>
		<div class="mt-10 flex flex-wrap items-center gap-4">
			<a
				href={resolve('/register')}
				class="inline-flex items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-6 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:bg-linear-to-br hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus"
			>
				Create account
			</a>
			<span
				class="inline-flex rounded-full border border-fe-light-500/30 bg-fe-light-500/10 px-5 py-2 text-sm text-cyan-200"
			>
				Frontend foundation ready
			</span>
		</div>
	</section>
</main>
