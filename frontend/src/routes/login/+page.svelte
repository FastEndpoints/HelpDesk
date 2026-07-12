<script lang="ts">
	import { enhance } from '$app/forms';
	import { resolve } from '$app/paths';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();

	let pending = $state(false);
</script>

<svelte:head>
	<title>Sign in · HelpDesk</title>
	<meta name="description" content="Sign in to your HelpDesk account." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-16">
	<section
		class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
	>
		<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">HelpDesk</p>
		<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Sign in</h1>
		<p class="mt-2 text-sm leading-6 text-fe-text-muted">
			Use the email and password for your verified HelpDesk account.
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
			{#if form?.errors.form?.[0]}
				<div
					class="rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-200"
					role="alert"
				>
					{form.errors.form[0]}
				</div>
			{/if}

			<div class="space-y-2">
				<label for="email" class="block text-sm font-medium text-fe-heading">Email</label>
				<input
					id="email"
					name="email"
					type="email"
					autocomplete="email"
					required
					maxlength={320}
					value={form?.values.email ?? ''}
					class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
					placeholder="you@example.com"
					aria-invalid={form?.errors.email ? 'true' : undefined}
					aria-describedby={form?.errors.email ? 'email-error' : undefined}
				/>
				{#if form?.errors.email?.[0]}
					<p id="email-error" class="text-sm text-red-300" role="alert">{form.errors.email[0]}</p>
				{/if}
			</div>

			<div class="space-y-2">
				<label for="password" class="block text-sm font-medium text-fe-heading">Password</label>
				<input
					id="password"
					name="password"
					type="password"
					autocomplete="current-password"
					required
					maxlength={128}
					class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
					placeholder="Your password"
					aria-invalid={form?.errors.password ? 'true' : undefined}
					aria-describedby={form?.errors.password ? 'password-error' : undefined}
				/>
				{#if form?.errors.password?.[0]}
					<p id="password-error" class="text-sm text-red-300" role="alert">
						{form.errors.password[0]}
					</p>
				{/if}
			</div>

			<button
				type="submit"
				disabled={pending}
				class="inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
			>
				{pending ? 'Signing in…' : 'Sign in'}
			</button>
		</form>

		<p class="mt-6 text-center text-sm text-fe-text-muted">
			No account?
			<a
				href={resolve('/register')}
				class="font-medium text-fe-light-500 transition-colors hover:text-white"
			>
				Create one
			</a>
		</p>
	</section>
</main>
