"use client";

import { Table, TableBody, TableCaption, TableCell, TableHead, TableHeader, TableRow } from "./ui/table";

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
					</TableRow>
				</TableHeader>
				<TableBody>
					{stations.map((station, index) => (
						<TableRow key={index}>
							<TableCell className="font-medium">{station.Name}</TableCell>
							<TableCell>{getStationType(station.Type)}</TableCell>
							<TableCell>{station.Address || "—"}</TableCell>
							<TableCell>{station.PostalCode || "—"}</TableCell>
							<TableCell>{station.Locality || "—"}</TableCell>
							<TableCell>{station.Province || "—"}</TableCell>
						</TableRow>
					))}
				</TableBody>
			</Table>
		</div>
	);
}
