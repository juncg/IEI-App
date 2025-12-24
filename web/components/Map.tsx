"use client";
import L from "leaflet";
import { useMemo } from "react";
import { MapContainer, Marker, Popup, TileLayer } from "react-leaflet";

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

interface MapProps {
	positions: { lat: number; lng: number; name?: string }[];
}

export default function Map({ positions }: MapProps) {
	// Calcular los límites del mapa para ajustar la vista a todos los marcadores
	const bounds = useMemo(() => {
		if (positions.length > 0) {
			const latLngs = positions.map((pos) => L.latLng(pos.lat, pos.lng));
			return L.latLngBounds(latLngs);
		}
		return null;
	}, [positions]);

	// Centro por defecto en Madrid si no hay posiciones
	const center: [number, number] | undefined = positions.length > 0 ? undefined : [40.4168, -3.7038];

	return (
		<MapContainer
			bounds={bounds || undefined}
			center={center}
			zoom={6}
			scrollWheelZoom={true}
			style={{ height: "500px", width: "100%", borderRadius: "0.5rem" }}>
			<TileLayer
				attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
				url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
			/>

			{positions.map((pos, index) => (
				<Marker key={index} position={[pos.lat, pos.lng]} icon={icon}>
					<Popup>{pos.name || `Estación ITV (${pos.lat.toFixed(4)}, ${pos.lng.toFixed(4)})`}</Popup>
				</Marker>
			))}
		</MapContainer>
	);
}
