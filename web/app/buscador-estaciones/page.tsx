"use client";

import SearchEstacionForm from "@/components/search-estacion-form";
import SearchResultsTable from "@/components/search-results-table";
import { H1 } from "@/components/ui/typography";
import "leaflet/dist/leaflet.css";
import dynamic from "next/dynamic";
import { useState } from "react";

const Map = dynamic(() => import("@/components/Map"), { ssr: false });

const valenciaPoints = [
	{ lat: 39.4699, lng: -0.3763, name: "Valencia Centro" },
	{ lat: 39.4702, lng: -0.3826, name: "Estación Norte" },
	{ lat: 39.4755, lng: -0.3791, name: "Ciudad de las Artes" },
];

export default function BuscadorEstaciones() {
	const [positions] = useState<{ lat: number; lng: number; name: string }[]>(valenciaPoints);

	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans">
			<main className="flex min-h-screen w-full max-w-7xl flex-col justify-between p-16 bg-white border-2 gap-8 shadow-2xl border-black rounded-xl m-8 items-start">
				<H1>Buscador de Estaciones ITV</H1>

				<div className="w-full">
					<SearchEstacionForm />
				</div>

				<div className="w-full">
					<SearchResultsTable
						stations={[
							{
								Address: "test",
								Latitude: 123,
								Locality: "Test",
								Longitude: 123,
								Name: "Test",
								PostalCode: "46000",
								Province: "Test",
								Type: 0,
							},
						]}
					/>
				</div>

				<div className="w-full mt-8">
					<Map positions={positions} />
				</div>
			</main>
		</div>
	);
}
