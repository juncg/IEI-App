import {
	Field,
	FieldDescription,
	FieldError,
	FieldGroup,
	FieldLabel,
	FieldLegend,
	FieldSet,
} from "@/components/ui/field";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { H1 } from "@/components/ui/typography";

export default function Home() {
	return (
		<div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans dark:bg-black">
			<main className="flex min-h-screen w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
				<H1>Buscador de Estaciones ITV</H1>

				<FieldSet>
					<FieldLegend>Profile</FieldLegend>
					<FieldDescription>This appears on invoices and emails.</FieldDescription>
					<FieldGroup>
						<Field>
							<FieldLabel htmlFor="name">Full name</FieldLabel>
							<Input id="name" autoComplete="off" placeholder="Evil Rabbit" />
							<FieldDescription>This appears on invoices and emails.</FieldDescription>
						</Field>
						<Field>
							<FieldLabel htmlFor="username">Username</FieldLabel>
							<Input id="username" autoComplete="off" aria-invalid />
							<FieldError>Choose another username.</FieldError>
						</Field>
						<Field orientation="horizontal">
							<Switch id="newsletter" />
							<FieldLabel htmlFor="newsletter">Subscribe to the newsletter</FieldLabel>
						</Field>
					</FieldGroup>
				</FieldSet>
			</main>
		</div>
	);
}
