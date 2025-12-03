import { Switch } from "@radix-ui/react-switch";
import { Field, FieldDescription, FieldError, FieldGroup, FieldLabel, FieldLegend, FieldSet } from "./ui/field";
import { Input } from "./ui/input";

export default function SearchEstacionForm() {
	return (
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
	);
}
