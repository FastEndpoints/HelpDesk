<script lang="ts">
	import { enhance } from '$app/forms';
	import { resolve } from '$app/paths';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let pending = $state(false);
</script>

<svelte:head>
	<title>Verify email · HelpDesk</title>
	<meta name="description" content="Confirm your HelpDesk email address to activate your account." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-16">
	{#if form?.success}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 text-center shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
			aria-live="polite"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				Verified
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Account verified</h1>
			<p class="mt-4 text-base leading-7 text-fe-text-muted">
				{form.message ?? 'Account verified.'}
			</p>
			<a
				href={resolve('/login')}
				class="mt-8 inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus"
			>
				Sign in
			</a>
			<a
				href={resolve('/')}
				class="mt-4 inline-flex text-sm font-medium text-fe-light-500 transition-colors hover:text-white"
			>
				Back to home
			</a>
		</section>
	{:else if !data.hasCode}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 text-center shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
			role="alert"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				Invalid link
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Missing verification code</h1>
			<p class="mt-4 text-base leading-7 text-fe-text-muted">
				This verification link is incomplete. Open the link from your welcome email, or register
				again.
			</p>
			<a
				href={resolve('/register')}
				class="mt-8 inline-flex text-sm font-medium text-fe-light-500 transition-colors hover:text-white"
			>
				Create account
			</a>
		</section>
	{:else}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				HelpDesk
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Verify your email</h1>
			<p class="mt-2 text-sm leading-6 text-fe-text-muted">
				Confirm this address to activate your account. Nothing is verified until you continue.
			</p>

			<form
				method="POST"
				class="mt-8 space-y-5"
				use:enhance={() => {
					pending = true;
					return async ({ update }) => {
						await update({ reset: false });
						pending = false;
					};
				}}
			>
				{#if form?.error}
					<div
						class="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-200"
						role="alert"
					>
						{form.error}
					</div>
				{/if}

				<button
					type="submit"
					disabled={pending}
					class="inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
				>
					{pending ? 'Verifying…' : 'Verify email'}
				</button>
			</form>
		</section>
	{/if}
</main>
