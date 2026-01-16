"use client";
import AppBreadcrumb from "@/components/app-breadcrumb";
import SearchEstacionForm from "@/components/search-estacion-form";
import SearchResultsTable from "@/components/search-results-table";
import { H1 } from "@/components/ui/typography";
import { API_URLS } from "@/lib/config";
import "leaflet/dist/leaflet.css";
import dynamic from "next/dynamic";
import { useEffect, useState } from "react";

const Map = dynamic(() => import("@/components/Map"), { ssr: false });

interface Station {
	name: string;
	type: number;
	address: string | null;
	postalCode: string | null;
	longitude: number | null;
	latitude: number | null;
	locality: string | null;
	province: string | null;
	description: string | null;
	schedule: string | null;
	contact: string | null;
	url: string | null;
}

export default function BuscadorEstaciones() {
	// Estado para almacenar los resultados de búsqueda
	const [stations, setStations] = useState<Station[]>([]);
	// Estado para las posiciones de todas las estaciones en el mapa
	const [positions, setPositions] = useState<{ lat: number; lng: number; name: string }[]>([]);
	// Estado para las posiciones de las estaciones filtradas/buscadas
	const [highlightedPositions, setHighlightedPositions] = useState<{ lat: number; lng: number; name: string }[]>([]);
	// Estado para saber si se ha realizado una búsqueda
	const [hasSearched, setHasSearched] = useState(false);

	// Cargar todas las estaciones con coordenadas al montar el componente (solo para el mapa)
	useEffect(() => {
		const loadInitialStations = async () => {
			try {
				const response = await fetch(`${API_URLS.SEARCH}/api/search/stations`);

				if (response.ok) {
					const data: Station[] = await response.json();

					// Extraer posiciones válidas para el mapa
					const newPositions = data
						.filter((s) => s.latitude && s.longitude)
						.map((s) => ({ lat: s.latitude!, lng: s.longitude!, name: s.name }));
					setPositions(newPositions);
				} else {
					console.error("Error obteniendo estaciones");
				}
			} catch (error) {
				console.error("Error:", error);
			}
		};

		loadInitialStations();
	}, []);

	// Manejar la búsqueda de estaciones con los filtros proporcionados
	const handleSearch = async (filters: {
		name?: string;
		type?: string;
		locality?: string;
		province?: string;
		postalCode?: string;
	}) => {
		setHasSearched(true);
		const query = new URLSearchParams();

		if (filters.name) query.append("name", filters.name);
		if (filters.type) query.append("type", filters.type);
		if (filters.locality) query.append("locality", filters.locality);
		if (filters.province) query.append("province", filters.province);
		if (filters.postalCode) query.append("postalCode", filters.postalCode);

		try {
			const response = await fetch(`${API_URLS.SEARCH}/api/search?${query.toString()}`);

			if (response.ok) {
				const data: Station[] = await response.json();
				setStations(data);

				// Actualizar las posiciones destacadas en rojo
				const newHighlightedPositions = data
					.filter((s) => s.latitude && s.longitude)
					.map((s) => ({ lat: s.latitude!, lng: s.longitude!, name: s.name }));
				setHighlightedPositions(newHighlightedPositions);
			} else {
				console.error("Error fetching stations");
			}
		} catch (error) {
			console.error("Error:", error);
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
					<SearchResultsTable stations={stations} hasSearched={hasSearched} />
				</div>

				<div className="w-full mt-8">
					<Map positions={positions} highlightedPositions={highlightedPositions} />
				</div>
			</main>
		</div>
	);
}
