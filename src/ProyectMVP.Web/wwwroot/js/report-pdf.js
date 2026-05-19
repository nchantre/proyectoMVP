/**
 * Genera y descarga un PDF del reporte de hurto (cliente).
 */
(function () {
  function formatDateTime(iso) {
    const d = new Date(iso);
    return d.toLocaleString("es-CO", { dateStyle: "long", timeStyle: "short" });
  }

  function formatShort(iso) {
    const d = new Date(iso);
    return d.toLocaleString("es-CO", { dateStyle: "short", timeStyle: "short" });
  }

  function line(doc, y, label, value) {
    doc.setFont("helvetica", "bold");
    doc.text(label, 14, y);
    doc.setFont("helvetica", "normal");
    const text = String(value ?? "—");
    const lines = doc.splitTextToSize(text, 175);
    doc.text(lines, 55, y);
    return y + Math.max(7, lines.length * 5);
  }

  function download(report, options) {
    if (!window.jspdf?.jsPDF) {
      alert("No se pudo cargar la librería PDF. Recargue la página (Ctrl+F5).");
      return;
    }

    const { jsPDF } = window.jspdf;
    const doc = new jsPDF({ unit: "mm", format: "a4" });
    const user = options?.user;
    const plate = options?.plate || report.plate;
    const sightings = options?.sightings || [];
    const generatedAt = new Date().toLocaleString("es-CO", {
      dateStyle: "long",
      timeStyle: "short"
    });

    let y = 18;

    doc.setFontSize(16);
    doc.setFont("helvetica", "bold");
    doc.setTextColor(157, 2, 8);
    doc.text("Reporte de hurto de vehículo", 105, y, { align: "center" });
    y += 8;

    doc.setFontSize(10);
    doc.setTextColor(80, 80, 80);
    doc.setFont("helvetica", "normal");
    doc.text("Sistema central ProyectMVP — consulta policial", 105, y, { align: "center" });
    y += 10;

    doc.setDrawColor(193, 18, 31);
    doc.setLineWidth(0.4);
    doc.line(14, y, 196, y);
    y += 8;

    doc.setTextColor(0, 0, 0);
    doc.setFontSize(11);
    y = line(doc, y, "N.º reporte:", `#${report.stolenVehicleReportId}`);
    y = line(doc, y, "Placa:", plate);
    y = line(doc, y, "Estado:", report.status);
    y = line(doc, y, "Fecha del hurto:", formatDateTime(report.theftDateUtc));
    y = line(doc, y, "Propietario (doc.):", report.ownerDocument);
    y = line(doc, y, "Lugar:", `${report.city ?? "—"}, ${report.country}`);
    y = line(doc, y, "Fuente:", report.sourceSystem || "—");

    if (report.brand) {
      const veh = [report.brand, report.vehicleLine, report.color, report.modelYear]
        .filter(Boolean)
        .join(" · ");
      y = line(doc, y, "Vehículo:", veh);
    }
    if (report.vehicleClass) {
      y = line(doc, y, "Clase:", report.vehicleClass);
    }

    y += 4;
    doc.setFont("helvetica", "bold");
    doc.setFontSize(12);
    doc.text("Resumen de avistamientos (consulta actual)", 14, y);
    y += 6;

    doc.setFont("helvetica", "normal");
    doc.setFontSize(10);
    if (!sightings.length) {
      doc.text("Sin avistamientos registrados para esta placa en el período consultado.", 14, y);
      y += 8;
    } else {
      const potential = sightings.filter((s) => s.isPotentialMatch).length;
      doc.text(
        `Total: ${sightings.length} avistamiento(s). Posible match con hurto: ${potential}.`,
        14,
        y
      );
      y += 7;

      const maxRows = 8;
      const rows = sightings.slice(-maxRows);
      doc.setFont("helvetica", "bold");
      doc.setFontSize(9);
      doc.text("Fecha / hora", 14, y);
      doc.text("Ciudad", 70, y);
      doc.text("Match", 150, y);
      y += 5;
      doc.setFont("helvetica", "normal");

      rows.forEach((s) => {
        if (y > 270) {
          return;
        }
        doc.text(formatShort(s.seenAtUtc), 14, y);
        doc.text(`${s.city ?? "—"}, ${s.country ?? ""}`.slice(0, 40), 70, y);
        doc.text(s.isPotentialMatch ? "Sí" : "No", 150, y);
        y += 5;
      });

      if (sightings.length > maxRows) {
        y += 2;
        doc.setTextColor(100, 100, 100);
        doc.text(`(Se muestran los últimos ${maxRows} de ${sightings.length})`, 14, y);
        doc.setTextColor(0, 0, 0);
        y += 5;
      }
    }

    y += 6;
    doc.setDrawColor(200, 200, 200);
    doc.line(14, y, 196, y);
    y += 6;

    doc.setFontSize(9);
    doc.setTextColor(90, 90, 90);
    if (user?.username) {
      doc.text(`Consulta realizada por: ${user.username} (${user.countryIsoCode || "—"})`, 14, y);
      y += 4;
    }
    doc.text(`Documento generado: ${generatedAt}`, 14, y);
    y += 4;
    doc.text("Documento informativo del POC. No constituye acto administrativo oficial.", 14, y);

    const safePlate = String(plate).replace(/[^A-Z0-9]/gi, "");
    doc.save(`reporte-hurto-${safePlate}-${report.stolenVehicleReportId}.pdf`);
  }

  window.ProyectMvpReportPdf = { download };
})();
