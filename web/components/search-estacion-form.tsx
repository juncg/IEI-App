"use client";
import { useState } from "react";
import { Button } from "./ui/button";
import { Field, FieldDescription, FieldGroup, FieldLabel, FieldLegend, FieldSet } from "./ui/field";
import { Input } from "./ui/input";

interface SearchFilters {
	name?: string;
	type?: string;
	locality?: string;
	province?: string;
	postalCode?: string;
}

interface SearchEstacionFormProps {
	onSearch?: (filters: SearchFilters) => void;
}

export default function SearchEstacionForm({ onSearch }: SearchEstacionFormProps) {
	// Estado de los filtros del formulario
	const [filters, setFilters] = useState<SearchFilters>({
		name: "",
		type: "",
		locality: "",
		province: "",
		postalCode: "",
	});

	// Actualizar un campo específico del filtro
	const handleInputChange = (field: keyof SearchFilters, value: string) => {
		setFilters((prev) => ({
			...prev,
			[field]: value,
		}));
	};

	// Manejar envío del formulario
	const handleSubmit = (e: React.FormEvent) => {
		e.preventDefault();

		// Filtrar solo los campos con valores no vacíos
		const activeFilters: SearchFilters = Object.entries(filters).reduce((acc, [key, value]) => {
			if (value && value.trim() !== "") {
				acc[key as keyof SearchFilters] = value.trim();
			}

			return acc;
		}, {} as SearchFilters);

		onSearch?.(activeFilters);
	};

	// Limpiar todos los filtros
	const handleReset = () => {
		setFilters({
			name: "",
			type: "",
			locality: "",
			province: "",
			postalCode: "",
		});
	};

	return (
		<form onSubmit={handleSubmit}>
			<FieldSet>
				<FieldLegend>Buscar Estaciones ITV</FieldLegend>
				<FieldDescription>
					Utiliza los filtros para buscar estaciones ITV por nombre, tipo, localidad, provincia o código
					postal.
				</FieldDescription>
				<FieldGroup>
					<Field>
						<FieldLabel htmlFor="name">Nombre de la estación</FieldLabel>
						<Input
							id="name"
							type="text"
							value={filters.name}
							onChange={(e) => handleInputChange("name", e.target.value)}
							placeholder="Ej: Estación ITV de Valencia"
							autoComplete="off"
						/>
						<FieldDescription>Busca por el nombre de la estación</FieldDescription>
					</Field>

					<Field>
						<FieldLabel htmlFor="type">Tipo de estación</FieldLabel>
						<select
							id="type"
							value={filters.type}
							onChange={(e) => handleInputChange("type", e.target.value)}
							className="flex h-9 w-full rounded-md border border-input bg-transparent px-3 py-1 text-base shadow-sm transition-colors file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50 md:text-sm">
							<option value="">Todos los tipos</option>
							<option value="0">Estación Fija</option>
							<option value="1">Estación Móvil</option>
							<option value="2">Otros</option>
						</select>
						<FieldDescription>Selecciona el tipo de estación</FieldDescription>
					</Field>

					<div className="grid grid-cols-1 md:grid-cols-2 gap-4">
						<Field>
							<FieldLabel htmlFor="locality">Localidad</FieldLabel>
							<Input
								id="locality"
								type="text"
								value={filters.locality}
								onChange={(e) => handleInputChange("locality", e.target.value)}
								placeholder="Ej: Valencia"
								autoComplete="off"
							/>
							<FieldDescription>Busca por localidad</FieldDescription>
						</Field>

						<Field>
							<FieldLabel htmlFor="province">Provincia</FieldLabel>
							<Input
								id="province"
								type="text"
								value={filters.province}
								onChange={(e) => handleInputChange("province", e.target.value)}
								placeholder="Ej: Valencia"
								autoComplete="off"
							/>
							<FieldDescription>Busca por provincia</FieldDescription>
						</Field>
					</div>

					<Field>
						<FieldLabel htmlFor="postalCode">Código Postal</FieldLabel>
						<Input
							id="postalCode"
							type="text"
							value={filters.postalCode}
							onChange={(e) => handleInputChange("postalCode", e.target.value)}
							placeholder="Ej: 46001"
							autoComplete="off"
							maxLength={5}
						/>
						<FieldDescription>Busca por código postal</FieldDescription>
					</Field>

					<div className="flex gap-3 pt-4">
						<Button type="submit" className="flex-1">
							Buscar
						</Button>
						<Button type="button" variant="outline" onClick={handleReset}>
							Limpiar filtros
						</Button>
					</div>
				</FieldGroup>
			</FieldSet>
		</form>
	);
}
