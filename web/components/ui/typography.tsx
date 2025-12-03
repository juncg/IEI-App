import { cn } from "@/lib/utils";
import { ReactNode } from "react";

interface TypographyProps {
	children: ReactNode;
	className?: string;
}

export function H1({ children, className }: TypographyProps) {
	return (
		<h1 className={cn("scroll-m-20 text-center text-4xl font-extrabold tracking-tight text-balance", className)}>
			{children}
		</h1>
	);
}

export function H2({ children, className }: TypographyProps) {
	return (
		<h2 className={cn("scroll-m-20 border-b pb-2 text-3xl font-semibold tracking-tight first:mt-0", className)}>
			{children}
		</h2>
	);
}

export function H3({ children, className }: TypographyProps) {
	return <h3 className={cn("scroll-m-20 text-2xl font-semibold tracking-tight", className)}>{children}</h3>;
}

export function H4({ children, className }: TypographyProps) {
	return <h4 className={cn("scroll-m-20 text-xl font-semibold tracking-tight", className)}>{children}</h4>;
}

export function P({ children, className }: TypographyProps) {
	return <h1 className={cn("leading-7 not-first:mt-6", className)}>{children}</h1>;
}
