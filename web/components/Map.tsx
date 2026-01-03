"use client";
import L from "leaflet";
import { useEffect, useMemo } from "react";
import { MapContainer, Marker, Popup, TileLayer, useMap } from "react-leaflet";

// Configuración del icono de marcador estándar de Leaflet
const icon = L.icon({
	iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
	iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
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
}

export default function Map({ positions }: MapProps) {
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

			{positions.map((pos, index) => (
				<Marker key={index} position={[pos.lat, pos.lng]} icon={icon}>
					<Popup>{pos.name || `Estación ITV (${pos.lat.toFixed(4)}, ${pos.lng.toFixed(4)})`}</Popup>
				</Marker>
			))}
		</MapContainer>
	);
}
