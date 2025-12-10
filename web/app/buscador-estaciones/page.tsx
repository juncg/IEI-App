"use client";

import AppBreadcrumb from "@/components/app-breadcrumb";
import SearchEstacionForm from "@/components/search-estacion-form";
import SearchResultsTable from "@/components/search-results-table";
import { H1 } from "@/components/ui/typography";
import "leaflet/dist/leaflet.css";
import dynamic from "next/dynamic";
import { useState } from "react";

const Map = dynamic(() => import("@/components/Map"), { ssr: false });

interface Station {
	Name: string;
	Type: number;
	Address: string | null;
	PostalCode: string | null;
	Longitude: number | null;
	Latitude: number | null;
	Locality: string | null;
	Province: string | null;
}

export default function BuscadorEstaciones() {
	const [stations, setStations] = useState<Station[]>([]);
	const [positions, setPositions] = useState<{ lat: number; lng: number; name: string }[]>([]);

	const handleSearch = async (filters: { name?: string; type?: string; locality?: string; province?: string; postalCode?: string }) => {
		const query = new URLSearchParams();
		if (filters.name) query.append('name', filters.name);
		if (filters.type) query.append('type', filters.type);
		if (filters.locality) query.append('locality', filters.locality);
		if (filters.province) query.append('province', filters.province);
		if (filters.postalCode) query.append('postalCode', filters.postalCode);

		try {
			const response = await fetch(`http://localhost:5005/api/search?${query.toString()}`);
			if (response.ok) {
				const data: Station[] = await response.json();
				setStations(data);
				const newPositions = data
					.filter(s => s.Latitude && s.Longitude)
					.map(s => ({ lat: s.Latitude!, lng: s.Longitude!, name: s.Name }));
				setPositions(newPositions);
			} else {
				console.error('Error fetching stations');
			}
		} catch (error) {
			console.error('Error:', error);
		}
	};

	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans">
			<main className="flex min-h-screen w-full max-w-7xl flex-col justify-between p-16 bg-white border-2 gap-8 shadow-2xl border-black rounded-xl m-8 items-start">
				<AppBreadcrumb />
				<H1>Buscador de Estaciones ITV</H1>

				<div className="w-full">
					<SearchEstacionForm onSearch={handleSearch} />
				</div>

				<div className="w-full">
					<SearchResultsTable stations={stations} />
				</div>

				<div className="w-full mt-8">
					<Map positions={positions} />
				</div>
			</main>
		</div>
	);
}
