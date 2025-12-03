"use client";

import SearchEstacionForm from "@/components/search-estacion-form";
import { H1 } from "@/components/ui/typography";
import "leaflet/dist/leaflet.css";
import dynamic from "next/dynamic";

const Map = dynamic(() => import("@/components/Map"), { ssr: false });

export default function BuscadorEstaciones() {
	const position: [number, number] = [40.4168, -3.7038];

	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans dark:bg-black">
			<main className="flex min-h-screen w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
				<H1>Buscador de Estaciones ITV</H1>

				<SearchEstacionForm />

				<div className="w-full mt-8">
					<Map position={position} />
				</div>
			</main>
		</div>
	);
}
