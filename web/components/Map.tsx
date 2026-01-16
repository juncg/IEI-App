"use client";
import L from "leaflet";
import { useEffect, useMemo } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";

// Configuración del icono azul (estaciones normales)
const blueIcon = L.icon({
	iconUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-blue.png",
	shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
	iconSize: [25, 41],
	iconAnchor: [12, 41],
	popupAnchor: [1, -34],
	shadowSize: [41, 41],
});

// Configuración del icono rojo (estaciones buscadas/filtradas)
const redIcon = L.icon({
	iconUrl: "https://raw.githubusercontent.com/pointhi/leaflet-color-markers/master/img/marker-icon-2x-red.png",
	shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
	iconSize: [25, 41],
	iconAnchor: [12, 41],
	popupAnchor: [1, -34],
	shadowSize: [41, 41],
});

// Componente para ajustar automáticamente los bounds del mapa cuando cambian las posiciones
function FitBounds({ positions }: { positions: { lat: number; lng: number; name?: string }[] }) {
	const map = useMap();

	useEffect(() => {
		if (positions.length > 0) {
			const latLngs = positions.map((pos) => L.latLng(pos.lat, pos.lng));
			const bounds = L.latLngBounds(latLngs);
			map.fitBounds(bounds, { padding: [20, 20] });
		} else {
			// Si no hay posiciones, centrar en Madrid
			map.setView([40.4168, -3.7038], 6);
		}
	}, [positions, map]);

	return null;
}

interface MapProps {
	positions: { lat: number; lng: number; name?: string }[];
	highlightedPositions?: { lat: number; lng: number; name?: string }[];
}

export default function Map({ positions, highlightedPositions = [] }: MapProps) {
	// Crear un Set con las coordenadas destacadas para búsqueda rápida
	const highlightedSet = useMemo(() => {
		return new Set(highlightedPositions.map((p) => `${p.lat},${p.lng}`));
	}, [highlightedPositions]);

	return (
		<MapContainer
			center={[40.4168, -3.7038]}
			zoom={6}
			scrollWheelZoom={true}
			style={{ height: "500px", width: "100%", borderRadius: "0.5rem" }}>
			<TileLayer
				attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
				url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
			/>

			<FitBounds positions={positions} />

			{positions.map((pos, index) => {
				const isHighlighted = highlightedSet.has(`${pos.lat},${pos.lng}`);
				return (
					<Marker key={index} position={[pos.lat, pos.lng]} icon={isHighlighted ? redIcon : blueIcon}>
						<Popup>{pos.name || `Estación ITV (${pos.lat.toFixed(4)}, ${pos.lng.toFixed(4)})`}</Popup>
					</Marker>
				);
			})}
		</MapContainer>
	);
}
