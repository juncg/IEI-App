"use client";
import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from "./ui/table";

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

interface SearchResultsTableProps {
	stations: Station[];
}

const getStationType = (type: number): string => {
	switch (type) {
		case 0:
			return "Estación Fija";
		case 1:
			return "Estación Móvil";
		case 2:
			return "Otros";
		default:
			return "Desconocido";
	}
};

export default function SearchResultsTable({ stations }: SearchResultsTableProps) {
	if (stations.length === 0) {
		return (
			<div className="text-center py-8 text-muted-foreground">
				No se encontraron estaciones. Intenta con otros filtros.
			</div>
		);
	}

	return (
		<div className="rounded-md border">
			<Table>
				<TableCaption>
					{stations.length} {stations.length === 1 ? "estación encontrada" : "estaciones encontradas"}
				</TableCaption>

				<TableHeader>
					<TableRow>
						<TableHead>Nombre</TableHead>
						<TableHead>Tipo</TableHead>
						<TableHead>Dirección</TableHead>
						<TableHead>C.P.</TableHead>
						<TableHead>Localidad</TableHead>
						<TableHead>Provincia</TableHead>
						<TableHead>Descripción</TableHead>
						<TableHead>Horario</TableHead>
						<TableHead>Contacto</TableHead>
						<TableHead>URL</TableHead>
					</TableRow>
				</TableHeader>

				<TableBody>
					{stations.map((station, index) => (
						<TableRow key={index}>
							<TableCell className="font-medium">{station.name}</TableCell>
							<TableCell>{getStationType(station.type)}</TableCell>
							<TableCell>{station.address || "—"}</TableCell>
							<TableCell>{station.postalCode || "—"}</TableCell>
							<TableCell>{station.locality || "—"}</TableCell>
							<TableCell>{station.province || "—"}</TableCell>
							<TableCell>{station.description || "—"}</TableCell>
							<TableCell>{station.schedule || "—"}</TableCell>
							<TableCell>{station.contact || "—"}</TableCell>
							<TableCell>
								{station.url ? (
									<a
										href={station.url}
										target="_blank"
										rel="noopener noreferrer"
										className="text-blue-600 hover:underline">
										{station.url}
									</a>
								) : (
									"—"
								)}
							</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</div>
	);
}
