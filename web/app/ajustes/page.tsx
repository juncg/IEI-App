/* eslint-disable @typescript-eslint/no-explicit-any */
"use client";

import AppBreadcrumb from "@/components/app-breadcrumb";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";

import { H1, H3, H4, P } from "@/components/ui/typography";
import { toast } from "sonner";
import "leaflet/dist/leaflet.css";
import { useState } from "react";

export default function CargaAlmacen() {
	const [selectedSources, setSelectedSources] = useState<string[]>([]);
	const [useSelenium, setUseSelenium] = useState(false);
	const [loadingData, setLoadingData] = useState(false);
	const [showConfirmLoad, setShowConfirmLoad] = useState(false);
	const [showConfirmClear, setShowConfirmClear] = useState(false);
	const [results, setResults] = useState<{
		loaded: number;
		repaired: number;
		discarded: number;
		repairedRecords: Array<{ source: string; name: string; locality: string; operations: Array<{ reason: string; operation: string }> }>;
		discardedRecords: Array<{ source: string; name: string; locality: string; reason: string }>;
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

	const performLoad = async () => {
		setLoadingData(true);
		setShowConfirmLoad(false);
		const sourceMap: { [key: string]: string } = {
			galicia: "GAL",
			valencia: "CV",
			catalunya: "CAT",
		};
		const apiSources = selectedSources.map(s => sourceMap[s]);

		try {
			const url = `http://localhost:5004/api/load${useSelenium ? "?validateExistingCoordinates=true" : ""}`;
			const response = await fetch(url, {
				method: "POST",
				headers: {
					"Content-Type": "application/json",
				},
				body: JSON.stringify(apiSources),
			});
			if (response.ok) {
				const data = await response.json();
				setResults({
					loaded: data.recordsLoadedCorrectly,
					repaired: data.recordsRepaired,
					discarded: data.recordsDiscarded,
					repairedRecords: data.repairedRecords.map((r: any) => ({
						source: r.dataSource,
						name: r.name,
						locality: r.locality,
						operations: r.operations.map((op: any) => ({
							reason: op.errorReason,
							operation: op.operationPerformed,
						})),
					})),
					discardedRecords: data.discardedRecords.map((r: any) => ({
						source: r.dataSource,
						name: r.name,
						locality: r.locality,
						reason: r.errorReason,
					})),
				});
				toast.success("Datos cargados correctamente");
			} else {
				console.error("Error loading data");
				toast.error("Error al cargar los datos");
			}
		} catch (error) {
			console.error("Error:", error);
		} finally {
			setLoadingData(false);
		}
	};

	const handleLoad = () => {
		setShowConfirmLoad(true);
	};

	const performClearData = async () => {
		setShowConfirmClear(false);
		try {
			const response = await fetch("http://localhost:5004/api/clear", {
				method: "POST",
			});
			if (response.ok) {
				console.log("Data cleared");
				toast.success("Datos borrados correctamente");
			} else {
				console.error("Error clearing data");
				toast.error("Error al borrar los datos");
			}
		} catch (error) {
			console.error("Error:", error);
			toast.error("Error al borrar los datos");
		}
	};

	const handleClearData = () => {
		setShowConfirmClear(true);
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
						<Button
							variant="default"
							onClick={handleLoad}
							disabled={selectedSources.length === 0 || loadingData}
							className="px-6 bg-gray-600 hover:bg-gray-700 cursor-pointer">
							{loadingData ? "Cargando..." : "Cargar"}
						</Button>
						<Button variant="destructive" onClick={handleClearData} className="px-6 cursor-pointer">
							Borrar almacén de datos
						</Button>
					</div>
					{results !== null && (
						<div className="space-y-4 pt-6">
							<H3>Resultados de la carga:</H3>

							<div className="border border-gray-300 p-4 rounded space-y-4 bg-gray-50">
								<div className="grid grid-cols-3 gap-4">
									<div className="text-sm">
										<strong>Cargados:</strong> {results.loaded}
									</div>
									<div className="text-sm">
										<strong>Reparados:</strong> {results.repaired}
									</div>
									<div className="text-sm">
										<strong>Descartados:</strong> {results.discarded}
									</div>
								</div>
								<div className="space-y-2">
									<H4 className="text-sm">Registros reparados:</H4>
									{results.repairedRecords.length > 0 ? (
										<div className="space-y-2">
											{results.repairedRecords.map((r, index) => (
												<div key={index} className="p-2 bg-white border rounded text-sm">
													<strong>Fuente:</strong> {r.source} | <strong>Nombre:</strong> {r.name} | <strong>Localidad:</strong> {r.locality}
													<div className="mt-1">
														<strong>Operaciones:</strong>
														<ul className="list-disc list-inside ml-2">
															{r.operations.map((op, opIndex) => (
																<li key={opIndex}>
																	{op.reason} → {op.operation}
																</li>
															))}
														</ul>
													</div>
												</div>
											))}
										</div>
									) : (
										<P className="text-sm text-gray-500">Ninguno</P>
									)}
								</div>
								<div className="space-y-2">
									<H4 className="text-sm">Registros descartados:</H4>
									{results.discardedRecords.length > 0 ? (
										<div className="space-y-2">
											{results.discardedRecords.map((r, index) => (
												<div key={index} className="p-2 bg-white border rounded text-sm">
													<strong>Fuente:</strong> {r.source} | <strong>Nombre:</strong> {r.name} | <strong>Localidad:</strong> {r.locality}
													<br />
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

				{/* Confirmation Dialog for Load */}
				<Dialog open={showConfirmLoad} onOpenChange={setShowConfirmLoad}>
					<DialogContent>
						<DialogHeader>
							<DialogTitle>Confirmar carga de datos</DialogTitle>
							<DialogDescription>
								¿Estás seguro de que quieres cargar los datos de las fuentes seleccionadas? Esta acción puede tardar varios minutos.
							</DialogDescription>
						</DialogHeader>
						<DialogFooter>
							<Button variant="outline" onClick={() => setShowConfirmLoad(false)} className="cursor-pointer">
								Cancelar
							</Button>
							<Button onClick={performLoad} className="bg-gray-600 hover:bg-gray-700 cursor-pointer">
								Confirmar
							</Button>
						</DialogFooter>
					</DialogContent>
				</Dialog>

				{/* Confirmation Dialog for Clear */}
				<Dialog open={showConfirmClear} onOpenChange={setShowConfirmClear}>
					<DialogContent>
						<DialogHeader>
							<DialogTitle>Confirmar borrado de datos</DialogTitle>
							<DialogDescription>
								¿Estás seguro de que quieres borrar todos los datos del almacén? Esta acción no se puede deshacer.
							</DialogDescription>
						</DialogHeader>
						<DialogFooter>
							<Button variant="outline" onClick={() => setShowConfirmClear(false)} className="cursor-pointer">
								Cancelar
							</Button>
							<Button variant="destructive" onClick={performClearData} className="cursor-pointer">
								Borrar
							</Button>
						</DialogFooter>
					</DialogContent>
				</Dialog>
			</main>
		</div>
	);
}
