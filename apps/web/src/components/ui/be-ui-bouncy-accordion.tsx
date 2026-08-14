"use client";

import { ChevronDown } from "lucide-react";
import { motion, useReducedMotion, type Transition } from "motion/react";
import {
  useCallback,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { cn } from "@/lib/utils";

export type BouncyAccordionItem = {
  id: string;
  title: ReactNode;
  description?: ReactNode;
  icon?: ReactNode;
  disabled?: boolean;
};

export type BouncyAccordionClassNames = {
  root?: string;
  item?: string;
  trigger?: string;
  icon?: string;
  title?: string;
  chevron?: string;
  content?: string;
  description?: string;
};

export interface BouncyAccordionProps {
  items: BouncyAccordionItem[];
  value?: string | null;
  defaultValue?: string | null;
  onValueChange?: (value: string | null) => void;
  collapsible?: boolean;
  className?: string;
  classNames?: BouncyAccordionClassNames;
}

const rowTransition: Transition = {
  type: "spring",
  duration: 0.55,
  bounce: 0.38,
};

const contentOpenTransition: Transition = {
  type: "spring",
  duration: 0.58,
  bounce: 0.32,
};

const contentCloseTransition: Transition = {
  type: "spring",
  duration: 0.46,
  bounce: 0.26,
};

const chevronTransition: Transition = {
  type: "spring",
  duration: 0.42,
  bounce: 0.28,
};

function useControllableValue({
  value,
  defaultValue,
  onValueChange,
}: Pick<BouncyAccordionProps, "value" | "defaultValue" | "onValueChange">) {
  const [internalValue, setInternalValue] = useState(defaultValue ?? null);
  const isControlled = value !== undefined;
  const currentValue = isControlled ? value : internalValue;

  const setValue = useCallback((nextValue: string | null) => {
    if (!isControlled) {
      setInternalValue(nextValue);
    }

    onValueChange?.(nextValue);
  }, [isControlled, onValueChange]);

  return [currentValue, setValue] as const;
}

function BouncyAccordionRow({
  classNames,
  contentId,
  item,
  onToggle,
  open,
  reduceMotion,
  triggerId,
}: {
  classNames?: BouncyAccordionClassNames;
  contentId: string;
  item: BouncyAccordionItem;
  onToggle: () => void;
  open: boolean;
  reduceMotion: boolean | null;
  triggerId: string;
}) {
  const contentReference = useRef<HTMLDivElement>(null);
  const [contentHeight, setContentHeight] = useState(0);

  useLayoutEffect(() => {
    const contentElement = contentReference.current;
    if (!contentElement) {
      return;
    }

    const updateHeight = () => setContentHeight(contentElement.offsetHeight);
    updateHeight();

    const resizeObserver = new ResizeObserver(updateHeight);
    resizeObserver.observe(contentElement);

    return () => resizeObserver.disconnect();
  }, []);

  return <motion.div
    data-state={open ? "open" : "closed"}
    initial={false}
    animate={{ borderRadius: 18 }}
    transition={reduceMotion ? { duration: 0 } : rowTransition}
    className={cn("overflow-hidden bg-card text-card-foreground", item.disabled && "opacity-50", classNames?.item)}>
    <button
      aria-controls={contentId}
      aria-expanded={open}
      className={cn(
        "flex min-h-[54px] w-full items-center gap-4 px-5 text-left outline-none transition-colors",
        "focus-visible:bg-muted/25 disabled:pointer-events-none",
        classNames?.trigger)
      }
      disabled={item.disabled}
      id={triggerId}
      onClick={onToggle}
      type="button">
      {item.icon && <span className={cn("grid h-7 w-7 shrink-0 place-items-center text-muted-foreground", classNames?.icon)}>{item.icon}</span>}
      <span className={cn("min-w-0 flex-1 text-[15px] font-medium text-foreground", classNames?.title)}>{item.title}</span>
      <motion.span
        aria-hidden="true"
        animate={{ rotate: open ? 180 : 0 }}
        className={cn("grid h-6 w-6 shrink-0 place-items-center text-muted-foreground", classNames?.chevron)}
        transition={reduceMotion ? { duration: 0 } : chevronTransition}>
        <ChevronDown className="h-4 w-4" />
      </motion.span>
    </button>
    <motion.div
      aria-hidden={!open}
      animate={{ height: open && item.description ? contentHeight : 0 }}
      className={cn("overflow-hidden", classNames?.content)}
      id={contentId}
      initial={false}
      role="region"
      transition={reduceMotion ? { duration: 0 } : open ? contentOpenTransition : contentCloseTransition}
      aria-labelledby={triggerId}>
      <motion.div
        animate={{ opacity: open ? 1 : 0 }}
        className="px-5 pb-5"
        ref={contentReference}
        transition={reduceMotion ? { duration: 0 } : { duration: 0.18, ease: [0.16, 1, 0.3, 1] }}>
        <div className={cn("text-[15px] leading-6 text-muted-foreground", classNames?.description)}>{item.description}</div>
      </motion.div>
    </motion.div>
  </motion.div>;
}

export function BouncyAccordion({
  className,
  classNames,
  collapsible = true,
  defaultValue = null,
  items,
  onValueChange,
  value,
}: BouncyAccordionProps) {
  const baseId = useId();
  const reduceMotion = useReducedMotion();
  const [activeValue, setActiveValue] = useControllableValue({ value, defaultValue, onValueChange });

  const toggleItem = useCallback((id: string) => {
    if (activeValue === id) {
      if (collapsible) {
        setActiveValue(null);
      }

      return;
    }

    setActiveValue(id);
  }, [activeValue, collapsible, setActiveValue]);

  return <div className={cn("w-full", className, classNames?.root)}>
    {items.map((item) => <BouncyAccordionRow
      classNames={classNames}
      contentId={`${baseId}-${item.id}-content`}
      item={item}
      key={item.id}
      onToggle={() => toggleItem(item.id)}
      open={activeValue === item.id}
      reduceMotion={reduceMotion}
      triggerId={`${baseId}-${item.id}-trigger`}
    />)}
  </div>;
}
