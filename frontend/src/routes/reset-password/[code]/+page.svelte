<script lang="ts">
	import { enhance } from '$app/forms';
	import { resolve } from '$app/paths';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	let pending = $state(false);
</script>

<svelte:head>
	<title>Reset password · HelpDesk</title>
	<meta name="description" content="Choose a new HelpDesk account password." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-16">
	{#if form?.success}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 text-center shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
			aria-live="polite"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				Password updated
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">You can sign in</h1>
			<p class="mt-4 text-base leading-7 text-fe-text-muted">
				{form.message ?? 'Password updated. You can sign in.'}
			</p>
			<a
				href={resolve('/login')}
				class="mt-8 inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus"
			>
				Sign in
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
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Missing reset code</h1>
			<p class="mt-4 text-base leading-7 text-fe-text-muted">
				This password reset link is incomplete. Open the link from your email, or request a new one.
			</p>
			<a
				href={resolve('/forgot-password')}
				class="mt-8 inline-flex text-sm font-medium text-fe-light-500 transition-colors hover:text-white"
			>
				Request a new link
			</a>
		</section>
	{:else}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				HelpDesk
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Choose a new password</h1>
			<p class="mt-2 text-sm leading-6 text-fe-text-muted">
				Enter a new password for your account. Nothing changes until you submit.
			</p>

			<form
				method="POST"
				action="?/reset"
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
					<label for="password" class="block text-sm font-medium text-fe-heading"
						>New password</label
					>
					<input
						id="password"
						name="password"
						type="password"
						autocomplete="new-password"
						required
						minlength={12}
						maxlength={128}
						class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
						placeholder="At least 12 characters"
						aria-invalid={form?.errors.password ? 'true' : undefined}
						aria-describedby={form?.errors.password ? 'password-error' : undefined}
					/>
					{#if form?.errors.password?.[0]}
						<p id="password-error" class="text-sm text-red-300" role="alert">
							{form.errors.password[0]}
						</p>
					{/if}
				</div>

				<div class="space-y-2">
					<label for="confirmPassword" class="block text-sm font-medium text-fe-heading"
						>Confirm password</label
					>
					<input
						id="confirmPassword"
						name="confirmPassword"
						type="password"
						autocomplete="new-password"
						required
						minlength={12}
						maxlength={128}
						class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
						placeholder="Re-enter password"
						aria-invalid={form?.errors.confirmPassword ? 'true' : undefined}
						aria-describedby={form?.errors.confirmPassword ? 'confirm-error' : undefined}
					/>
					{#if form?.errors.confirmPassword?.[0]}
						<p id="confirm-error" class="text-sm text-red-300" role="alert">
							{form.errors.confirmPassword[0]}
						</p>
					{/if}
				</div>

				<button
					type="submit"
					disabled={pending}
					class="inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
				>
					{pending ? 'Updating…' : 'Update password'}
				</button>
			</form>
		</section>
	{/if}
</main>
