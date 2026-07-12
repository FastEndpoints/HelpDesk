<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';
	import type { LayoutData } from './+layout.server';

	let { children, data }: { children: import('svelte').Snippet; data: LayoutData } = $props();

	const user = $derived(data.user);
	const registerActive = $derived(page.url.pathname === '/register');
	const loginActive = $derived(page.url.pathname === '/login');

	const initials = $derived.by(() => {
		if (!user?.displayName) return '?';
		const parts = user.displayName.trim().split(/\s+/).filter(Boolean);
		if (parts.length === 0) return '?';
		if (parts.length === 1) return parts[0]!.slice(0, 1).toUpperCase();
		return `${parts[0]!.slice(0, 1)}${parts[1]!.slice(0, 1)}`.toUpperCase();
	});
</script>

<svelte:head><link rel="icon" href={favicon} /></svelte:head>

<div class="flex min-h-screen flex-col">
	<header class="sticky top-0 z-40 border-b border-fe-border bg-fe-dark-800/88 backdrop-blur-md">
		<div class="mx-auto flex h-16 max-w-[1460px] items-center justify-between gap-6 px-6">
			<a
				href={resolve('/')}
				class="text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase transition-colors hover:text-white"
			>
				HelpDesk
			</a>
			<nav class="flex items-center gap-4 text-sm">
				{#if user}
					<span
						class="flex items-center gap-2.5 rounded-md px-2 py-1.5 text-fe-heading"
						data-testid="shell-profile"
					>
						{#if user.pictureUrl}
							<img
								src={user.pictureUrl}
								alt=""
								class="h-8 w-8 rounded-full object-cover ring-1 ring-fe-border"
							/>
						{:else}
							<span
								class="flex h-8 w-8 items-center justify-center rounded-full bg-fe-dark-500 text-xs font-semibold text-fe-light-500 ring-1 ring-fe-border"
								aria-hidden="true"
							>
								{initials}
							</span>
						{/if}
						<span class="max-w-[12rem] truncate font-medium">{user.displayName}</span>
					</span>
				{:else}
					<a
						href={resolve('/login')}
						class={[
							'rounded-md px-3 py-2 font-medium transition-colors',
							loginActive
								? 'bg-[linear-gradient(90deg,rgb(0_223_255_/_0.16),transparent)] text-fe-light-500'
								: 'text-fe-heading hover:text-white'
						]}
					>
						Sign in
					</a>
					<a
						href={resolve('/register')}
						class={[
							'rounded-md px-3 py-2 font-medium transition-colors',
							registerActive
								? 'bg-[linear-gradient(90deg,rgb(0_223_255_/_0.16),transparent)] text-fe-light-500'
								: 'text-fe-heading hover:text-white'
						]}
					>
						Create account
					</a>
				{/if}
			</nav>
		</div>
	</header>

	<div class="flex-1">
		{@render children()}
	</div>
</div>
