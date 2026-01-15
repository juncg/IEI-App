// Configuración de URLs del backend
// Para desarrollo local: usa localhost
// Para acceso remoto: usa la IP del servidor (ej: "http://192.168.1.100")

const getBaseUrl = () => {
	// Si estás en el navegador, usa la IP del servidor
	if (typeof window !== "undefined") {
		// Puedes cambiar esto por la IP de tu servidor cuando accedas remotamente
		// Por ejemplo: return "http://192.168.1.100"
		return window.location.hostname === "localhost" ? "http://localhost" : `http://${window.location.hostname}`;
	}
	return "http://localhost";
};

const BASE_URL = getBaseUrl();

export const API_URLS = {
	SEARCH: `${BASE_URL}:5005`,
	LOAD: `${BASE_URL}:5004`,
	CAT: `${BASE_URL}:5001`,
	CV: `${BASE_URL}:5002`,
	GAL: `${BASE_URL}:5003`,
};
