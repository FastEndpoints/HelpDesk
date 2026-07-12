<script lang="ts">
	import '../app.css';
	import favicon from '$lib/assets/favicon.svg';
	import { resolve } from '$app/paths';
	import { page } from '$app/state';

	let { children } = $props();

	const registerActive = $derived(page.url.pathname === '/register');
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
			</nav>
		</div>
	</header>

	<div class="flex-1">
		{@render children()}
	</div>
</div>
