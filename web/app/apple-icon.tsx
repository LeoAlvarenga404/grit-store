import { ImageResponse } from "next/og";

export const size = { width: 180, height: 180 };
export const contentType = "image/png";

export default function Icon() {
  return new ImageResponse(
    (
      <div style={{ width: "100%", height: "100%", background: "#111111", display: "flex", alignItems: "center", justifyContent: "center" }}>
        <svg width="96" height="96" viewBox="0 0 100 100">
          <polyline points="84,11 30,11 11,30 11,70 30,89 89,89 89,50 55,50" fill="none" stroke="#ffffff" strokeWidth="22" />
        </svg>
      </div>
    ),
    size,
  );
}
