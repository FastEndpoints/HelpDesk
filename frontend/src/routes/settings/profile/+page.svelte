<script lang="ts">
	import { enhance } from '$app/forms';
	import { invalidateAll } from '$app/navigation';
	import type { ActionData, PageData } from './$types';

	let { data, form }: { data: PageData; form: ActionData } = $props();

	const profile = $derived(form?.profile ?? data.profile);

	let editing = $state(false);
	let pending = $state(false);
	let pendingAction = $state<'update' | 'uploadPicture' | 'deletePicture' | null>(null);
	let fileInput = $state<HTMLInputElement | null>(null);

	const displayNameValue = $derived(
		form?.action === 'update' && form.values.displayName !== undefined
			? form.values.displayName
			: profile.displayName
	);

	const initials = $derived.by(() => {
		const parts = profile.displayName.trim().split(/\s+/).filter(Boolean);
		if (parts.length === 0) return '?';
		if (parts.length === 1) return parts[0]!.slice(0, 1).toUpperCase();
		return `${parts[0]!.slice(0, 1)}${parts[1]!.slice(0, 1)}`.toUpperCase();
	});

	$effect(() => {
		if (form?.success) {
			editing = false;
		}
	});

	function startEdit() {
		editing = true;
	}

	function cancelEdit() {
		editing = false;
		pending = false;
		pendingAction = null;
		if (fileInput) fileInput.value = '';
	}
</script>

<svelte:head>
	<title>Profile · HelpDesk</title>
	<meta name="description" content="View and update your HelpDesk profile." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-start justify-center px-6 py-16">
	<section
		class="w-full max-w-lg rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
		data-testid="profile-page"
	>
		<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">Account</p>
		<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Profile</h1>
		<p class="mt-2 text-sm leading-6 text-fe-text-muted">
			Your account details and profile picture.
		</p>

		{#if form?.success && form.message}
			<div
				class="mt-6 rounded-md border border-fe-light-500/30 bg-fe-light-500/10 px-3 py-2 text-sm text-fe-light-500"
				role="status"
				data-testid="profile-success"
			>
				{form.message}
			</div>
		{/if}

		{#if form?.errors.form?.[0]}
			<div
				class="mt-6 rounded-md border border-red-500/30 bg-red-500/10 px-3 py-2 text-sm text-red-200"
				role="alert"
				data-testid="profile-form-error"
			>
				{form.errors.form[0]}
			</div>
		{/if}

		{#if !editing}
			<div class="mt-8 space-y-6" data-testid="profile-readonly">
				<div class="flex items-center gap-4">
					{#if profile.pictureUrl}
						<img
							src={profile.pictureUrl}
							alt=""
							class="h-20 w-20 rounded-full object-cover ring-1 ring-fe-border"
							data-testid="profile-picture"
						/>
					{:else}
						<span
							class="flex h-20 w-20 items-center justify-center rounded-full bg-fe-dark-500 text-xl font-semibold text-fe-light-500 ring-1 ring-fe-border"
							aria-hidden="true"
							data-testid="profile-initials"
						>
							{initials}
						</span>
					{/if}
					<div class="min-w-0">
						<p
							class="truncate text-lg font-medium text-fe-heading"
							data-testid="profile-display-name"
						>
							{profile.displayName}
						</p>
						<p class="mt-1 truncate text-sm text-fe-text-muted" data-testid="profile-email">
							{profile.email}
						</p>
					</div>
				</div>

				<dl
					class="space-y-3 rounded-lg border border-fe-border bg-fe-dark-800/50 px-4 py-4 text-sm"
				>
					<div class="flex justify-between gap-4">
						<dt class="text-fe-text-muted">Status</dt>
						<dd class="font-medium text-fe-heading" data-testid="profile-status">
							{profile.status}
						</dd>
					</div>
				</dl>

				<button
					type="button"
					class="inline-flex w-full items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus"
					data-testid="profile-edit"
					onclick={startEdit}
				>
					Edit profile
				</button>
			</div>
		{:else}
			<div class="mt-8 space-y-8" data-testid="profile-edit-mode">
				<form
					method="POST"
					action="?/update"
					class="space-y-5"
					use:enhance={() => {
						pending = true;
						pendingAction = 'update';
						return async ({ result, update }) => {
							await update({ reset: false });
							pending = false;
							pendingAction = null;
							if (result.type === 'success') {
								await invalidateAll();
							}
						};
					}}
				>
					<div class="space-y-2">
						<label for="displayName" class="block text-sm font-medium text-fe-heading">
							Display name
						</label>
						<input
							id="displayName"
							name="displayName"
							type="text"
							required
							maxlength={100}
							value={displayNameValue}
							class="w-full rounded-md border border-fe-border bg-fe-dark-800 px-3 py-2.5 text-sm text-fe-text placeholder:text-fe-text-muted/60 focus:border-fe-light-500 focus:ring-1 focus:ring-fe-light-500 focus:outline-none"
							placeholder="Your display name"
							aria-invalid={form?.errors.displayName ? 'true' : undefined}
							aria-describedby={form?.errors.displayName ? 'displayName-error' : undefined}
							data-testid="profile-display-name-input"
						/>
						{#if form?.errors.displayName?.[0]}
							<p id="displayName-error" class="text-sm text-red-300" role="alert">
								{form.errors.displayName[0]}
							</p>
						{/if}
					</div>

					<div class="space-y-2">
						<label for="email-readonly" class="block text-sm font-medium text-fe-heading"
							>Email</label
						>
						<input
							id="email-readonly"
							type="email"
							value={profile.email}
							disabled
							class="w-full cursor-not-allowed rounded-md border border-fe-border bg-fe-dark-800/60 px-3 py-2.5 text-sm text-fe-text-muted"
							data-testid="profile-email-readonly"
						/>
						<p class="text-xs text-fe-text-muted">Email cannot be changed here.</p>
					</div>

					<div class="flex gap-3">
						<button
							type="submit"
							disabled={pending}
							class="inline-flex flex-1 items-center justify-center rounded-md bg-linear-to-br from-fe-light-600 to-fe-blue-600 px-4 py-3 text-sm font-semibold tracking-wide text-white uppercase shadow-[0_8px_24px_rgb(9_110_235_/_0.35)] transition-all hover:from-fe-blue-600 hover:to-fe-light-600 focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
							data-testid="profile-save-name"
						>
							{pending && pendingAction === 'update' ? 'Saving…' : 'Save name'}
						</button>
						<button
							type="button"
							disabled={pending}
							class="inline-flex items-center justify-center rounded-md border border-fe-border px-4 py-3 text-sm font-medium text-fe-heading transition-colors hover:border-fe-light-500/40 hover:text-white disabled:cursor-not-allowed disabled:opacity-60"
							data-testid="profile-cancel-edit"
							onclick={cancelEdit}
						>
							Cancel
						</button>
					</div>
				</form>

				<div class="border-t border-fe-border pt-8">
					<h2 class="text-sm font-medium text-fe-heading">Profile picture</h2>
					<p class="mt-1 text-sm text-fe-text-muted">
						PNG or JPG, up to 5 MB. Saved as a 300×300 crop.
					</p>

					<div class="mt-4 flex items-center gap-4">
						{#if profile.pictureUrl}
							<img
								src={profile.pictureUrl}
								alt=""
								class="h-16 w-16 rounded-full object-cover ring-1 ring-fe-border"
							/>
						{:else}
							<span
								class="flex h-16 w-16 items-center justify-center rounded-full bg-fe-dark-500 text-sm font-semibold text-fe-light-500 ring-1 ring-fe-border"
								aria-hidden="true"
							>
								{initials}
							</span>
						{/if}
					</div>

					<form
						method="POST"
						action="?/uploadPicture"
						enctype="multipart/form-data"
						class="mt-4 space-y-4"
						use:enhance={() => {
							pending = true;
							pendingAction = 'uploadPicture';
							return async ({ result, update }) => {
								await update({ reset: false });
								pending = false;
								pendingAction = null;
								if (fileInput) fileInput.value = '';
								if (result.type === 'success') {
									await invalidateAll();
								}
							};
						}}
					>
						<input type="hidden" name="displayName" value={profile.displayName} />
						<div class="space-y-2">
							<label for="file" class="block text-sm font-medium text-fe-heading"
								>Choose image</label
							>
							<input
								id="file"
								name="file"
								type="file"
								accept="image/png,image/jpeg,.png,.jpg,.jpeg"
								required
								bind:this={fileInput}
								class="block w-full text-sm text-fe-text file:mr-4 file:rounded-md file:border-0 file:bg-fe-dark-500 file:px-3 file:py-2 file:text-sm file:font-medium file:text-fe-heading hover:file:bg-fe-dark-500/80"
								aria-invalid={form?.errors.file ? 'true' : undefined}
								aria-describedby={form?.errors.file ? 'file-error' : undefined}
								data-testid="profile-picture-input"
							/>
							{#if form?.errors.file?.[0]}
								<p id="file-error" class="text-sm text-red-300" role="alert">
									{form.errors.file[0]}
								</p>
							{/if}
						</div>
						<button
							type="submit"
							disabled={pending}
							class="inline-flex w-full items-center justify-center rounded-md border border-fe-light-500/40 bg-fe-light-500/10 px-4 py-3 text-sm font-semibold tracking-wide text-fe-light-500 uppercase transition-colors hover:bg-fe-light-500/15 focus-visible:outline-fe-focus disabled:cursor-not-allowed disabled:opacity-60"
							data-testid="profile-upload-picture"
						>
							{pending && pendingAction === 'uploadPicture' ? 'Uploading…' : 'Upload picture'}
						</button>
					</form>

					{#if profile.pictureUrl}
						<form
							method="POST"
							action="?/deletePicture"
							class="mt-3"
							use:enhance={() => {
								pending = true;
								pendingAction = 'deletePicture';
								return async ({ result, update }) => {
									await update({ reset: false });
									pending = false;
									pendingAction = null;
									if (result.type === 'success') {
										await invalidateAll();
									}
								};
							}}
						>
							<input type="hidden" name="displayName" value={profile.displayName} />
							<button
								type="submit"
								disabled={pending}
								class="inline-flex w-full items-center justify-center rounded-md border border-red-500/30 px-4 py-3 text-sm font-medium text-red-200 transition-colors hover:border-red-400/50 hover:bg-red-500/10 disabled:cursor-not-allowed disabled:opacity-60"
								data-testid="profile-delete-picture"
							>
								{pending && pendingAction === 'deletePicture' ? 'Removing…' : 'Remove picture'}
							</button>
						</form>
					{/if}
				</div>
			</div>
		{/if}
	</section>
</main>
