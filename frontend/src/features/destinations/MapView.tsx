import { useEffect, useRef } from 'react';
import L from 'leaflet';

const DEFAULT_ZOOM = 14;
const TILE_URL = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
const TILE_ATTRIBUTION =
  '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors';

// Avoids importing Leaflet's default marker image assets (the well-known bundler-breaks-marker-icons issue) by using a plain divIcon.
const markerIcon = L.divIcon({
  className: 'trip-planner-map-marker',
  html: '<span class="material-symbols-outlined" style="font-size:36px;color:#DC2626;" aria-hidden="true">location_on</span>',
  iconSize: [36, 36],
  iconAnchor: [18, 36],
});

interface MapViewProps {
  latitude: number;
  longitude: number;
  name: string;
  label?: string | null;
}

// plain Leaflet, not react-leaflet, for React 19 peer-dep safety
export function MapView({ latitude, longitude, name, label }: MapViewProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const container = containerRef.current;
    if (!container) return;

    const map = L.map(container, {
      center: [latitude, longitude],
      zoom: DEFAULT_ZOOM,
    });

    L.tileLayer(TILE_URL, { attribution: TILE_ATTRIBUTION, maxZoom: 19 }).addTo(map);
    L.marker([latitude, longitude], { icon: markerIcon }).addTo(map).bindPopup(name);

    return () => {
      map.remove();
    };
  }, [latitude, longitude, name]);

  return (
    <div className="space-y-2">
      <div
        ref={containerRef}
        role="region"
        aria-label={`Map showing ${name}`}
        className="w-full aspect-[16/9] rounded-xl overflow-hidden elevation-l1 border border-outline-variant/30"
      />
      {label && <p className="text-on-surface-variant text-label-sm">{label}</p>}
    </div>
  );
}
