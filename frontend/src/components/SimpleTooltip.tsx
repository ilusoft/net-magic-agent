import {
  type ReactNode,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import { createPortal } from "react-dom";

interface SimpleTooltipProps {
  content: ReactNode;
  children: ReactNode;
}

export function SimpleTooltip({ content, children }: SimpleTooltipProps) {
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<{
    top: number;
    left: number;
  } | null>(null);
  const anchorRef = useRef<HTMLSpanElement | null>(null);

  const updatePosition = useCallback(() => {
    const anchor = anchorRef.current;
    if (!anchor) {
      return;
    }

    const rect = anchor.getBoundingClientRect();
    setPosition({
      top: rect.bottom + window.scrollY + 8,
      left: rect.left + rect.width / 2 + window.scrollX,
    });
  }, []);

  useEffect(() => {
    if (!open) {
      return;
    }

    updatePosition();

    const handleReposition = () => updatePosition();
    window.addEventListener("scroll", handleReposition, true);
    window.addEventListener("resize", handleReposition);

    return () => {
      window.removeEventListener("scroll", handleReposition, true);
      window.removeEventListener("resize", handleReposition);
    };
  }, [open, updatePosition]);

  return (
    <>
      <span
        ref={anchorRef}
        className="inline-flex"
        onMouseEnter={() => setOpen(true)}
        onMouseLeave={() => setOpen(false)}
        onFocus={() => setOpen(true)}
        onBlur={() => setOpen(false)}
      >
        {children}
      </span>
      {open && position && typeof document !== "undefined"
        ? createPortal(
            <span
              role="tooltip"
              className="normal-case pointer-events-none fixed z-[9999] min-w-[160px] max-w-xs -translate-x-1/2 whitespace-pre-wrap rounded-md border border-border/60 bg-background/95 px-3 py-1 text-[10px] font-medium text-foreground shadow-lg"
              style={{ top: position.top, left: position.left }}
            >
              {content}
            </span>,
            document.body
          )
        : null}
    </>
  );
}
