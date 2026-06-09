import Image from "next/image";
import type { CSSProperties } from "react";
import { visualAssets, type VisualIconName } from "@/lib/visual-assets";

export function VelaLogoMark({ size = 48 }: { size?: number }) {
  return <Image src={visualAssets.brand.mark} alt="" width={size} height={size} priority unoptimized className="shrink-0" />;
}

export function VelaWordmark() {
  return <Image src={visualAssets.brand.wordmark} alt="Vela Training OS" width={142} height={36} priority unoptimized className="h-9 w-auto" />;
}

export function AssetIcon({
  name,
  className = "size-4",
}: {
  name: VisualIconName;
  className?: string;
}) {
  const style = {
    WebkitMaskImage: `url(${visualAssets.icons[name]})`,
    maskImage: `url(${visualAssets.icons[name]})`,
    WebkitMaskRepeat: "no-repeat",
    maskRepeat: "no-repeat",
    WebkitMaskPosition: "center",
    maskPosition: "center",
    WebkitMaskSize: "contain",
    maskSize: "contain",
  } satisfies CSSProperties;

  return <span aria-hidden="true" className={`inline-block bg-current ${className}`} style={style} />;
}
