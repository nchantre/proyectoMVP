let map;
let markersLayer;
let routeLayer;

function initMap() {
  map = L.map("map").setView([6.2442, -75.5812], 12);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: "&copy; OpenStreetMap"
  }).addTo(map);

  markersLayer = L.layerGroup().addTo(map);
  routeLayer = L.layerGroup().addTo(map);
}

function setStatus(message, isError = false) {
  const el = document.getElementById("status");
  el.textContent = message;
  el.style.color = isError ? "#ffb4a2" : "rgba(255,255,255,0.95)";
}

function formatDateTime(iso) {
  const d = new Date(iso);
  return d.toLocaleString("es-CO", { dateStyle: "short", timeStyle: "short" });
}

function clearResults() {
  markersLayer.clearLayers();
  routeLayer.clearLayers();
  document.getElementById("events-list").innerHTML = "";
  document.getElementById("stolen-reports-list").innerHTML = "";
  document.getElementById("stolen-reports-panel").hidden = true;
}

window.clearMapResults = clearResults;

function renderStolenReports(plate, reports) {
  const panel = document.getElementById("stolen-reports-panel");
  const list = document.getElementById("stolen-reports-list");
  list.innerHTML = "";

  if (!reports || reports.length === 0) {
    panel.hidden = true;
    return;
  }

  panel.hidden = false;
  reports.forEach((r) => {
    const li = document.createElement("li");
    li.className = `stolen-item status-${(r.status || "").toLowerCase()}`;
    li.innerHTML = `
      <span class="stolen-status">${r.status}</span>
      <span class="stolen-date">Hurto: ${formatDateTime(r.theftDateUtc)}</span>
      <span class="stolen-doc">Doc. propietario: ${r.ownerDocument}</span>
      <span class="stolen-place">${r.city ?? "—"}, ${r.country}</span>
      ${r.brand ? `<span class="stolen-vehicle">${r.brand} ${r.vehicleLine ?? ""} · ${r.color ?? ""} ${r.modelYear ?? ""}</span>` : ""}
      ${r.sourceSystem ? `<span class="stolen-source">Fuente: ${r.sourceSystem}</span>` : ""}
    `;
    list.appendChild(li);
  });
}

async function loadStolenReports(plate) {
  const response = await window.ProyectMvpAuth.apiFetch(
    `/plates/${encodeURIComponent(plate)}/stolen-reports`
  );
  if (!response.ok) {
    throw new Error(`Reportes de hurto: error ${response.status}`);
  }
  const data = await response.json();
  renderStolenReports(plate, data.reports || []);
}

function renderSightings(plate, sightings) {
  const list = document.getElementById("events-list");
  const latLngs = [];

  sightings.forEach((s, index) => {
    const lat = Number(s.latitude);
    const lng = Number(s.longitude);
    latLngs.push([lat, lng]);

    const popupHtml = `
      <strong>${plate}</strong><br/>
      ${formatDateTime(s.seenAtUtc)}<br/>
      ${s.city ?? ""}, ${s.country}<br/>
      ${s.isPotentialMatch ? '<span class="match-badge">Posible hurto</span><br/>' : ""}
      ${s.evidenceUrl ? `<a href="${s.evidenceUrl}" target="_blank" rel="noopener">Ver evidencia</a>` : ""}
    `;

    const marker = L.circleMarker([lat, lng], {
      radius: 8,
      color: s.isPotentialMatch ? "#c1121f" : "#0d3b66",
      fillColor: s.isPotentialMatch ? "#e5383b" : "#2a9d8f",
      fillOpacity: 0.85,
      weight: 2
    }).bindPopup(popupHtml);

    marker.addTo(markersLayer);

    const li = document.createElement("li");
    li.dataset.index = String(index);
    li.innerHTML = `
      <span class="time">${formatDateTime(s.seenAtUtc)}</span>
      <span class="place">${s.city ?? "—"}, ${s.country}</span>
      ${s.isPotentialMatch ? '<span class="match">Posible match hurto</span>' : ""}
      ${s.evidenceUrl ? `<br/><a href="${s.evidenceUrl}" target="_blank" rel="noopener">Evidencia</a>` : ""}
    `;
    li.addEventListener("click", () => {
      map.setView([lat, lng], 15);
      marker.openPopup();
      document.querySelectorAll("#events-list li").forEach((el) => el.classList.remove("active"));
      li.classList.add("active");
    });
    list.appendChild(li);
  });

  if (latLngs.length >= 2) {
    L.polyline(latLngs, { color: "#e76f51", weight: 4, opacity: 0.8 }).addTo(routeLayer);
  }

  if (latLngs.length > 0) {
    map.fitBounds(L.latLngBounds(latLngs), { padding: [40, 40] });
  }
}

async function searchPlate(plate) {
  if (!window.ProyectMvpAuth.isLoggedIn()) {
    setStatus("Inicie sesión para consultar matrículas.", true);
    return;
  }

  const normalized = plate.trim().toUpperCase();
  if (!normalized) {
    return;
  }

  setStatus(`Buscando ${normalized}...`);
  clearResults();

  try {
    const [sightingsResponse, stolenResponse] = await Promise.all([
      window.ProyectMvpAuth.apiFetch(`/plates/${encodeURIComponent(normalized)}/sightings`),
      window.ProyectMvpAuth.apiFetch(`/plates/${encodeURIComponent(normalized)}/stolen-reports`)
    ]);

    if (!sightingsResponse.ok) {
      throw new Error(`Avistamientos: error ${sightingsResponse.status}`);
    }

    const data = await sightingsResponse.json();
    const sightings = [...(data.sightings || [])].sort(
      (a, b) => new Date(a.seenAtUtc) - new Date(b.seenAtUtc)
    );

    if (stolenResponse.ok) {
      const stolenData = await stolenResponse.json();
      renderStolenReports(normalized, stolenData.reports || []);
    }

    if (sightings.length === 0) {
      setStatus(`Sin avistamientos para ${normalized}.`);
      return;
    }

    renderSightings(normalized, sightings);
    const stolenCount = document.querySelectorAll("#stolen-reports-list li").length;
    const stolenMsg = stolenCount > 0 ? ` · ${stolenCount} reporte(s) de hurto` : "";
    setStatus(`${sightings.length} avistamiento(s) para ${normalized}${stolenMsg}.`);
  } catch (err) {
    console.error(err);
    setStatus(err.message || "No se pudo conectar con la API. ¿Está ejecutándose ProyectMVP.Api?", true);
  }
}

window.onPoliceLoggedIn = (plate) => {
  document.getElementById("plate-input").value = plate;
  searchPlate(plate);
};

document.getElementById("search-form").addEventListener("submit", (e) => {
  e.preventDefault();
  searchPlate(document.getElementById("plate-input").value);
});

initMap();

const defaultPlate = new URLSearchParams(window.location.search).get("plate") || "ABC123";
document.getElementById("plate-input").value = defaultPlate;
if (window.ProyectMvpAuth.isLoggedIn()) {
  searchPlate(defaultPlate);
}
