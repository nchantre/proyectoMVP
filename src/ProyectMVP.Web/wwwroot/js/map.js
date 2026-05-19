let map;
let markersLayer;
let routeLayer;
let hotspotsLayer;
let theftReportLayer;
let theftReportMarker;
let lastSearchContext = { plate: "", sightings: [] };

const HOTSPOT_COLORS = {
  theft: { stroke: "#9d0208", fill: "#e5383b" },
  sighting: { stroke: "#0d3b66", fill: "#2a9d8f" },
  potential_match: { stroke: "#c1121f", fill: "#ff758f" }
};

function initMap() {
  map = L.map("map").setView([6.2442, -75.5812], 12);
  L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
    maxZoom: 19,
    attribution: "&copy; OpenStreetMap"
  }).addTo(map);

  markersLayer = L.layerGroup().addTo(map);
  routeLayer = L.layerGroup().addTo(map);
  hotspotsLayer = L.layerGroup().addTo(map);
  theftReportLayer = L.layerGroup().addTo(map);
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
  theftReportLayer.clearLayers();
  document.getElementById("events-list").innerHTML = "";
  document.getElementById("stolen-reports-list").innerHTML = "";
  document.getElementById("stolen-reports-panel").hidden = true;
}

window.clearMapResults = clearResults;

function hotspotsEnabled() {
  const el = document.getElementById("hotspots-toggle");
  return el && el.checked;
}

function clearHotspots() {
  hotspotsLayer.clearLayers();
}

function renderHotspots(hotspots) {
  clearHotspots();
  if (!hotspotsEnabled() || !hotspots?.length) {
    return;
  }

  hotspots.forEach((h) => {
    const lat = Number(h.latitude);
    const lng = Number(h.longitude);
    const colors = HOTSPOT_COLORS[h.category] || HOTSPOT_COLORS.sighting;
    const radius = 14 + Number(h.intensity || 0) * 28;

    L.circle([lat, lng], {
      radius,
      color: colors.stroke,
      fillColor: colors.fill,
      fillOpacity: 0.25 + Number(h.intensity || 0) * 0.35,
      weight: 2
    })
      .bindPopup(
        `<strong>${h.label}</strong><br/>${h.count} evento(s)<br/>Intensidad: ${h.intensity}<br/><em>${h.category}</em>`
      )
      .addTo(hotspotsLayer);
  });
}

async function loadHotspots() {
  if (!window.ProyectMvpAuth.isLoggedIn() || !hotspotsEnabled()) {
    clearHotspots();
    return;
  }

  try {
    const response = await window.ProyectMvpAuth.apiFetch("/analytics/hotspots?category=all");
    if (!response.ok) {
      return;
    }
    const data = await response.json();
    renderHotspots(data.hotspots || []);
  } catch (err) {
    console.warn("Hotspots no disponibles", err);
  }
}

window.loadHotspots = loadHotspots;

document.getElementById("hotspots-toggle")?.addEventListener("change", () => {
  if (hotspotsEnabled()) {
    loadHotspots();
  } else {
    clearHotspots();
  }
});

function renderStolenReports(plate, reports, errorMessage) {
  const panel = document.getElementById("stolen-reports-panel");
  const list = document.getElementById("stolen-reports-list");
  list.innerHTML = "";
  panel.hidden = false;

  if (errorMessage) {
    const li = document.createElement("li");
    li.className = "stolen-item stolen-empty";
    li.innerHTML = `<span class="stolen-empty-msg">${errorMessage}</span>`;
    list.appendChild(li);
    return;
  }

  if (!reports || reports.length === 0) {
    const li = document.createElement("li");
    li.className = "stolen-item stolen-empty";
    li.innerHTML = `<span class="stolen-empty-msg">Sin reportes de hurto para <strong>${plate}</strong> en su país.</span>`;
    list.appendChild(li);
    return;
  }

  const titleEl = document.getElementById("stolen-reports-title");
  if (titleEl) {
    titleEl.textContent = reports.length === 1 ? "Reporte de hurto" : `Reportes de hurto (${reports.length})`;
  }

  reports.forEach((r) => {
    const li = document.createElement("li");
    li.className = `stolen-item status-${(r.status || "").toLowerCase()}`;
    li.innerHTML = `
      <article class="stolen-card">
        <div class="stolen-card-header">
          <span class="stolen-plate">${r.plate || plate}</span>
          <span class="stolen-report-id">#${r.stolenVehicleReportId}</span>
        </div>
        <span class="stolen-status">Estado: ${r.status}</span>
        <span class="stolen-date"><strong>Fecha hurto:</strong> ${formatDateTime(r.theftDateUtc)}</span>
        <span class="stolen-doc"><strong>Propietario:</strong> ${r.ownerDocument}</span>
        <span class="stolen-place"><strong>Lugar:</strong> ${r.city ?? "—"}, ${r.country}</span>
        ${r.brand ? `<span class="stolen-vehicle"><strong>Vehículo:</strong> ${r.brand} ${r.vehicleLine ?? ""} · ${r.color ?? ""} · ${r.modelYear ?? ""}</span>` : ""}
        ${r.sourceSystem ? `<span class="stolen-source"><strong>Fuente:</strong> ${r.sourceSystem}</span>` : ""}
        <div class="stolen-card-actions">
          <button type="button" class="stolen-map-btn">Ver en mapa</button>
          <button type="button" class="stolen-pdf-btn">Descargar PDF</button>
        </div>
      </article>
    `;
    li.querySelector(".stolen-map-btn")?.addEventListener("click", (e) => {
      e.stopPropagation();
      window.focusTheftReportOnMap?.();
    });
    li.querySelector(".stolen-pdf-btn")?.addEventListener("click", (e) => {
      e.stopPropagation();
      window.ProyectMvpReportPdf?.download(r, {
        plate: plate || r.plate,
        sightings: lastSearchContext.sightings,
        user: window.ProyectMvpAuth.getUser()
      });
    });
    li.addEventListener("click", () => {
      document.querySelectorAll("#stolen-reports-list .stolen-item").forEach((el) => el.classList.remove("active"));
      li.classList.add("active");
      window.focusTheftReportOnMap?.();
    });
    list.appendChild(li);
  });

  return reports;
}

function theftMarkerPopupHtml(r) {
  return `
    <strong>Reporte de hurto #${r.stolenVehicleReportId}</strong><br/>
    Placa: <strong>${r.plate}</strong><br/>
    Estado: <strong>${r.status}</strong><br/>
    ${r.city ?? ""}, ${r.country}<br/>
    Fecha hurto: ${formatDateTime(r.theftDateUtc)}<br/>
    ${r.brand ? `${r.brand} ${r.vehicleLine ?? ""} · ${r.color ?? ""} · ${r.modelYear ?? ""}` : ""}
  `;
}

function showTheftOnMap(reports, sightings) {
  theftReportLayer.clearLayers();
  theftReportMarker = null;
  if (!reports?.length) {
    return;
  }

  let lat;
  let lng;
  if (sightings?.length) {
    lat = sightings.reduce((s, x) => s + Number(x.latitude), 0) / sightings.length;
    lng = sightings.reduce((s, x) => s + Number(x.longitude), 0) / sightings.length;
    lat += 0.004;
  } else {
    lat = 6.2442;
    lng = -75.5812;
  }

  const r = reports[0];
  const icon = L.divIcon({
    className: "theft-marker-icon",
    html: '<div class="theft-marker-pin">HURTO</div>',
    iconSize: [56, 28],
    iconAnchor: [28, 14]
  });

  theftReportMarker = L.marker([lat, lng], { icon, zIndexOffset: 1000 })
    .bindPopup(theftMarkerPopupHtml(r))
    .addTo(theftReportLayer);
}

window.focusTheftReportOnMap = () => {
  if (!theftReportMarker) {
    return;
  }
  const latLng = theftReportMarker.getLatLng();
  map.setView(latLng, Math.max(map.getZoom(), 14));
  theftReportMarker.openPopup();
};

async function loadStolenReports(plate) {
  try {
    const response = await window.ProyectMvpAuth.apiFetch(
      `/plates/${encodeURIComponent(plate)}/stolen-reports`
    );
    if (!response.ok) {
      const err = await response.json().catch(() => ({}));
      renderStolenReports(
        plate,
        [],
        err.message || `No se pudieron cargar reportes (HTTP ${response.status}). ¿Sesión activa?`
      );
      return [];
    }
    const data = await response.json();
    const reports = data.reports || [];
    renderStolenReports(plate, reports);
    return reports;
  } catch (err) {
    console.error(err);
    renderStolenReports(plate, [], "Error de conexión al cargar reportes de hurto.");
    return [];
  }
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
  lastSearchContext = { plate: normalized, sightings: [] };

  try {
    const stolenReports = await loadStolenReports(normalized);

    const sightingsResponse = await window.ProyectMvpAuth.apiFetch(
      `/plates/${encodeURIComponent(normalized)}/sightings`
    );

    if (!sightingsResponse.ok) {
      throw new Error(`Avistamientos: error ${sightingsResponse.status}`);
    }

    const data = await sightingsResponse.json();
    const sightings = [...(data.sightings || [])].sort(
      (a, b) => new Date(a.seenAtUtc) - new Date(b.seenAtUtc)
    );

    if (sightings.length > 0) {
      renderSightings(normalized, sightings);
    } else {
      document.getElementById("events-list").innerHTML =
        '<li class="stolen-empty"><span class="stolen-empty-msg">Sin avistamientos para esta placa.</span></li>';
    }

    showTheftOnMap(stolenReports, sightings);
    lastSearchContext = { plate: normalized, sightings };

    const stolenMsg =
      stolenReports.length > 0 ? ` · ${stolenReports.length} reporte(s) de hurto` : " · sin reportes de hurto";
    const sightMsg =
      sightings.length > 0
        ? `${sightings.length} avistamiento(s)`
        : "Sin avistamientos";
    setStatus(`${sightMsg} para ${normalized}${stolenMsg}.`);
  } catch (err) {
    console.error(err);
    setStatus(err.message || "No se pudo conectar con la API. ¿Está ejecutándose ProyectMVP.Api?", true);
  }
}

window.onPoliceLoggedIn = (plate) => {
  document.getElementById("plate-input").value = plate;
  loadHotspots();
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
  loadHotspots();
  searchPlate(defaultPlate);
}
