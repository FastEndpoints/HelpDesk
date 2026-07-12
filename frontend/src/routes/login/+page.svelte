<script lang="ts">
	import { resolve } from '$app/paths';
</script>

<svelte:head>
	<title>Sign in · HelpDesk</title>
	<meta name="description" content="Sign in to your HelpDesk account." />
</svelte:head>

<main class="flex min-h-[calc(100vh-4rem)] items-center justify-center px-6 py-16">
	<section
		class="w-full max-w-md rounded-xl border border-fe-border bg-fe-dark-600 px-8 py-10 text-center shadow-[0_20px_60px_rgb(0_0_0_/_0.35)]"
	>
		<p class="mb-3 text-sm font-semibold tracking-[0.25em] text-fe-light-500 uppercase">HelpDesk</p>
		<h1 class="text-2xl font-semibold tracking-tight text-fe-heading">Sign in</h1>
		<p class="mt-4 text-base leading-7 text-fe-text-muted">
			Login is not available yet. If you just verified your email, you can return home for now.
		</p>
		<a
			href={resolve('/')}
			class="mt-8 inline-flex text-sm font-medium text-fe-light-500 transition-colors hover:text-white"
		>
			Back to home
		</a>
	</section>
</main>
