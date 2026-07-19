<script lang="ts">
	import { enhance } from '$app/forms';
	import { resolve } from '$app/paths';
	import {
		formatResendCountdown,
		isResendButtonDisabled,
		RESEND_COOLDOWN_MS,
		resendButtonLabel,
		shouldShowResendCountdown,
		shouldStartResendCooldown
	} from '$lib/resend-cooldown';
	import type { ActionData } from './$types';

	let { form }: { form: ActionData } = $props();

	let pending = $state(false);
	let resendPending = $state(false);
	let cooldownEndsAt = $state<number | null>(null);
	let nowMs = $state(Date.now());

	const remainingMs = $derived(
		cooldownEndsAt == null ? 0 : Math.max(0, cooldownEndsAt - nowMs)
	);
	const showResendCountdown = $derived(shouldShowResendCountdown({ remainingMs }));
	const resendDisabled = $derived(
		isResendButtonDisabled({
			pending: resendPending,
			remainingMs,
			email: form?.values.email
		})
	);
	const resendLabel = $derived(
		resendButtonLabel({ pending: resendPending, resendSuccess: form?.resendSuccess })
	);

	function startResendCooldown(durationMs = RESEND_COOLDOWN_MS) {
		cooldownEndsAt = Date.now() + durationMs;
		nowMs = Date.now();
	}

	// Register issues verification immediately — start client cooldown with the success card.
	// Also covers full-page POST hydration when resend already succeeded.
	$effect(() => {
		if (
			shouldStartResendCooldown({
				surface: 'register',
				success: form?.success,
				resendSuccess: form?.resendSuccess,
				cooldownAlreadyStarted: cooldownEndsAt != null
			})
		) {
			startResendCooldown();
		}
	});

	$effect(() => {
		if (!showResendCountdown) return;
		const id = setInterval(() => {
			nowMs = Date.now();
		}, 1000);
		return () => clearInterval(id);
	});
</script>

<svelte:head>
	<title>Create account · HelpDesk</title>
	<meta name="description" content="Register a HelpDesk identity with email and password." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-16">
	{#if form?.success}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 text-center shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
			aria-live="polite"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				Almost there
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Check your email</h1>
			<p class="mt-4 text-base leading-7 text-fe-text-muted">
				{form.resendSuccess
					? (form.message ?? 'If an account needs verification, we sent a link.')
					: (form.message ??
						'Signup successful. Please check your email for a verification link.')}
			</p>
			<p class="mt-3 text-sm leading-6 text-fe-text-muted">
				Didn’t get it? Check spam, then resend the verification email.
			</p>

			{#if form.errors.resend?.[0]}
				<div
					class="mt-4 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-200"
					role="alert"
				>
					{form.errors.resend[0]}
				</div>
			{/if}

			<form
				method="POST"
				action="?/resend"
				class="mt-6"
				use:enhance={() => {
					resendPending = true;
					return async ({ result, update }) => {
						await update({ reset: false });
						resendPending = false;
						if (result.type === 'success' && result.data?.resendSuccess) {
							startResendCooldown();
						}
					};
				}}
			>
				<input type="hidden" name="email" value={form.values.email} />
				{#if showResendCountdown}
					<p class="mb-3 text-sm leading-6 text-fe-text-muted" aria-live="polite">
						You can send again in {formatResendCountdown(remainingMs)}.
					</p>
				{/if}
				<button
					type="submit"
					disabled={resendDisabled}
					class="inline-flex w-full items-center justify-center rounded-md border border-fe-border bg-fe-dark-800 px-4 py-3 text-sm font-semibold tracking-wide text-fe-heading uppercase transition-colors hover:border-fe-light-500/40 hover:text-white focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
				>
					{resendLabel}
				</button>
			</form>

			<a
				href={resolve('/')}
				class="mt-8 inline-flex text-sm font-medium text-fe-light-500 transition-colors hover:text-white"
			>
				Back to home
			</a>
		</section>
	{:else}
		<section
			class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
		>
			<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">
				HelpDesk
			</p>
			<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Create account</h1>
			<p class="mt-2 text-sm leading-6 text-fe-text-muted">
				Register with your email. You’ll verify the account before signing in.
			</p>

			<form
				method="POST"
				action="?/register"
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
						autocomplete="new-password"
						required
						minlength={12}
						maxlength={128}
						class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
						placeholder="At least 12 characters"
						aria-invalid={form?.errors.password ? 'true' : undefined}
						aria-describedby={form?.errors.password ? 'password-error' : 'password-hint'}
					/>
					{#if form?.errors.password?.[0]}
						<p id="password-error" class="text-sm text-red-300" role="alert">
							{form.errors.password[0]}
						</p>
					{:else}
						<p id="password-hint" class="text-xs text-fe-text-muted">12–128 characters.</p>
					{/if}
				</div>

				<div class="space-y-2">
					<label for="confirmPassword" class="block text-sm font-medium text-fe-heading">
						Confirm password
					</label>
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
					{pending ? 'Creating account…' : 'Create account'}
				</button>
			</form>
		</section>
	{/if}
</main>
