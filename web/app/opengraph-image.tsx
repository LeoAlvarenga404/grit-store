import { ImageResponse } from "next/og";

export const alt = "GRIT, roupas e tênis esportivos";
export const size = { width: 1200, height: 630 };
export const contentType = "image/png";

// The same letters as components/logo.tsx, redrawn for the renderer, which takes plain SVG shapes.
export default function Image() {
  return new ImageResponse(
    (
      <div style={{ width: "100%", height: "100%", background: "#111111", color: "#ffffff", display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", gap: 48 }}>
        <svg width="840" height="240" viewBox="0 0 350 100">
          <g fill="none" stroke="#ffffff" strokeWidth="22">
            <polyline points="84,11 30,11 11,30 11,70 30,89 89,89 89,50 55,50" />
            <polyline points="125,11 176,11 197,32 197,38 176,59 125,59" />
          </g>
          <g fill="#ffffff">
            <rect x="114" y="0" width="22" height="100" />
            <polygon points="152,52 178,52 210,100 182,100" />
            <rect x="224" y="0" width="22" height="100" />
            <rect x="260" y="0" width="90" height="22" />
            <rect x="294" y="0" width="22" height="100" />
          </g>
        </svg>
        <div style={{ fontSize: 40, letterSpacing: 6, textTransform: "uppercase", opacity: 0.7 }}>Roupas e tênis esportivos</div>
      </div>
    ),
    size,
  );
}
