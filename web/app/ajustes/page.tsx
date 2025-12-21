"use client";

import AppBreadcrumb from "@/components/app-breadcrumb";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";

import { H1, H3, H4, P } from "@/components/ui/typography";
import "leaflet/dist/leaflet.css";
import { useState } from "react";

export default function CargaAlmacen() {
	const [selectedSources, setSelectedSources] = useState<string[]>([]);
	const [useSelenium, setUseSelenium] = useState(false);
	const [loadingData, setLoadingData] = useState(false);
	const [results, setResults] = useState<{
		success: number;
		errors: Array<{ source: string; name: string; locality: string; reason: string; operation: string }>;
		rejected: Array<{ source: string; name: string; locality: string; reason: string }>;
	} | null>(null);

	const sources = [
		{ id: "galicia", label: "Galicia" },
		{ id: "valencia", label: "Comunitat Valenciana" },
		{ id: "catalunya", label: "Catalunya" },
	];

	const handleSelectAll = () => {
		if (selectedSources.length === sources.length) {
			setSelectedSources([]);
		} else {
			setSelectedSources(sources.map((s) => s.id));
		}
	};

	const handleSourceToggle = (sourceId: string) => {
		setSelectedSources((prev) =>
			prev.includes(sourceId) ? prev.filter((id) => id !== sourceId) : [...prev, sourceId]
		);
	};

	const handleLoad = async () => {
		setLoadingData(true);
		const sourceMap: { [key: string]: string } = {
			galicia: "GAL",
			valencia: "CV",
			catalunya: "CAT",
		};
		const apiSources = selectedSources.map(s => sourceMap[s]);

		try {
			const response = await fetch("http://localhost:5004/api/load", {
				method: "POST",
				headers: {
					"Content-Type": "application/json",
				},
				body: JSON.stringify(apiSources),
			});
			if (response.ok) {
				const data = await response.json();
				setResults({
					success: data.recordsLoadedCorrectly,
					errors: data.repairedRecords.map((r: any) => ({
						source: r.dataSource,
						name: r.name,
						locality: r.locality,
						reason: r.errorReason,
						operation: r.operationPerformed,
					})),
					rejected: data.discardedRecords.map((r: any) => ({
						source: r.dataSource,
						name: r.name,
						locality: r.locality,
						reason: r.errorReason,
					})),
				});
			} else {
				console.error("Error loading data");
			}
		} catch (error) {
			console.error("Error:", error);
		} finally {
			setLoadingData(false);
		}
	};

	const handleCancel = () => {
		setSelectedSources([]);
		setResults(null);
	};

	const handleClearData = async () => {
		try {
			const response = await fetch("http://localhost:5004/api/clear", {
				method: "POST",
			});
			if (response.ok) {
				console.log("Data cleared");
			} else {
				console.error("Error clearing data");
			}
		} catch (error) {
			console.error("Error:", error);
		}
	};

	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans">
			<main className="flex w-full max-w-7xl flex-col justify-between p-16 bg-white border-2 gap-8 shadow-2xl border-black rounded-xl m-8 items-start">
				<AppBreadcrumb />
				<H1>Carga del almacén de datos</H1>

				<div className="w-full flex gap-8">
					<div className="flex-1 space-y-4">
						<H3>Seleccione fuente:</H3>

						<div className="space-y-3 ml-4">
							<div className="flex items-center space-x-2">
								<Checkbox
									id="select-all"
									checked={selectedSources.length === sources.length}
									onCheckedChange={handleSelectAll}
								/>
								<Label htmlFor="select-all" className="cursor-pointer">
									Seleccionar todas
								</Label>
							</div>

							{sources.map((source) => (
								<div key={source.id} className="flex items-center space-x-2">
									<Checkbox
										id={source.id}
										checked={selectedSources.includes(source.id)}
										onCheckedChange={() => handleSourceToggle(source.id)}
									/>
									<Label htmlFor={source.id} className="cursor-pointer">
										{source.label}
									</Label>
								</div>
							))}
						</div>
					</div>

					<div className="flex-1 space-y-4">
						<H3>Opciones adicionales:</H3>

						<div className="flex items-center space-x-2 ml-4">
							<Checkbox
								id="use-selenium"
								checked={useSelenium}
								onCheckedChange={(checked) => setUseSelenium(checked as boolean)}
							/>
							<Label htmlFor="use-selenium" className="cursor-pointer">
								Comprobar las coordenadas con Selenium
							</Label>
						</div>
					</div>
				</div>

				<div className="w-full space-y-6">
					{" "}
					<div className="flex gap-4 pt-4">
						<Button variant="outline" onClick={handleCancel} className="px-6">
							Cancelar
						</Button>
						<Button
							variant="default"
							onClick={handleLoad}
							disabled={selectedSources.length === 0 || loadingData}
							className="px-6 bg-gray-600 hover:bg-gray-700">
							{loadingData ? "Cargando..." : "Cargar"}
						</Button>
						<Button variant="destructive" onClick={handleClearData} className="px-6">
							Borrar almacén de datos
						</Button>
					</div>
					{results !== null && (
						<div className="space-y-4 pt-6">
							<H3>Resultados de la carga:</H3>

							<div className="border border-gray-300 p-4 rounded space-y-4 bg-gray-50">
								<P className="text-sm">
									Número de registros cargados correctamente: <strong>{results.success}</strong>
								</P>
								<div className="space-y-2">
									<H4 className="text-sm">Registros con errores y reparados:</H4>
									{results.errors.length > 0 ? (
										<div className="space-y-2">
											{results.errors.map((e, index) => (
												<div key={index} className="p-2 bg-white border rounded text-sm">
													<strong>Fuente:</strong> {e.source} | <strong>Nombre:</strong> {e.name} | <strong>Localidad:</strong> {e.locality}<br />
													<strong>Error:</strong> {e.reason} | <strong>Operación:</strong> {e.operation}
												</div>
											))}
										</div>
									) : (
										<P className="text-sm text-gray-500">Ninguno</P>
									)}
								</div>
								<div className="space-y-2">
									<H4 className="text-sm">Registros con errores y rechazados:</H4>
									{results.rejected.length > 0 ? (
										<div className="space-y-2">
											{results.rejected.map((r, index) => (
												<div key={index} className="p-2 bg-white border rounded text-sm">
													<strong>Fuente:</strong> {r.source} | <strong>Nombre:</strong> {r.name} | <strong>Localidad:</strong> {r.locality}<br />
													<strong>Error:</strong> {r.reason}
												</div>
											))}
										</div>
									) : (
										<P className="text-sm text-gray-500">Ninguno</P>
									)}
								</div>
							</div>
						</div>
					)}
				</div>
			</main>
		</div>
	);
}
