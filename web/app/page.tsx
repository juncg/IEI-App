import { Button } from "@/components/ui/button";
import { H1 } from "@/components/ui/typography";
import Link from "next/link";

export default function Home() {
	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans dark:bg-black">
			<main className="flex gap-4 w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
				<H1>App IEI-ITV</H1>

				<div className="flex gap-4">
					<Link href="/buscador-estaciones">
						<Button className="cursor-pointer">Buscador de estaciones</Button>
					</Link>

					<Link href="/ajustes">
						<Button className="cursor-pointer">Carga del almacén de datos</Button>
					</Link>
				</div>
			</main>
		</div>
	);
}
