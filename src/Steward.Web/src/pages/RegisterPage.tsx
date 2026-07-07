import { zodResolver } from "@hookform/resolvers/zod";
import { Check, Eye, EyeOff, Wrench, X } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate } from "react-router";
import { z } from "zod";
import { OAuthButtons, useOAuthSectionVisible } from "@/components/auth/OAuthButtons";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/context/AuthContext";
import { applyValidationErrors, getApiErrorMessage } from "@/lib/apiErrors";
import { cn } from "@/lib/utils";

const PASSWORD_MIN_LENGTH = 8;
const PASSWORD_NON_ALPHANUMERIC = /[^a-zA-Z0-9]/;

const schema = z
  .object({
    email: z.string().email("Enter a valid email address"),
    password: z
      .string()
      .min(PASSWORD_MIN_LENGTH, "Password must be at least 8 characters")
      .regex(PASSWORD_NON_ALPHANUMERIC, "Password must contain at least one non-alphanumeric character"),
    confirmPassword: z.string(),
    displayName: z.string().min(1, "Display name is required"),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  });

type FormValues = z.infer<typeof schema>;

interface PasswordRequirement {
  label: string;
  isSatisfied: (password: string) => boolean;
}

const PASSWORD_REQUIREMENTS: PasswordRequirement[] = [
  { label: "At least 8 characters", isSatisfied: (password) => password.length >= PASSWORD_MIN_LENGTH },
  {
    label: "At least one non-alphanumeric character",
    isSatisfied: (password) => PASSWORD_NON_ALPHANUMERIC.test(password),
  },
];

export function RegisterPage() {
  const { register } = useAuth();
  const navigate = useNavigate();
  const [formError, setFormError] = useState<string | null>(null);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const showOAuthSection = useOAuthSectionVisible();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { email: "", password: "", confirmPassword: "", displayName: "" },
  });

  const password = form.watch("password");

  async function onSubmit(values: FormValues) {
    setFormError(null);
    try {
      await register({ email: values.email, password: values.password, displayName: values.displayName });
      navigate("/");
    } catch (error) {
      if (!applyValidationErrors(error, form.setError)) {
        setFormError(getApiErrorMessage(error, "Couldn't create your account."));
      }
    }
  }

  return (
    <div className="mx-auto flex min-h-svh w-full max-w-[420px] flex-col justify-center gap-8 px-6 py-10">
      <div className="text-center">
        <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-primary-foreground">
          <Wrench className="h-6 w-6" />
        </div>
        <h1 className="text-h2">Maintenance Tracker</h1>
        <p className="mt-1 text-small text-muted-foreground">Keep everything running smoothly</p>
      </div>

      <Card className="shadow-[0_4px_24px_rgba(0,0,0,0.04)]">
        <CardContent className="space-y-6 py-8">
          <h2 className="text-h2">Create an account</h2>
          {showOAuthSection && (
            <>
              <OAuthButtons />
              <div className="flex items-center gap-3 text-caption font-medium uppercase tracking-wide text-muted-foreground">
                <div className="h-px flex-1 bg-border" />
                or continue with
                <div className="h-px flex-1 bg-border" />
              </div>
            </>
          )}
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField
                control={form.control}
                name="displayName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Display name</FormLabel>
                    <FormControl>
                      <Input autoComplete="name" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Email</FormLabel>
                    <FormControl>
                      <Input type="email" autoComplete="email" {...field} />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Password</FormLabel>
                    <div className="relative">
                      <FormControl>
                        <Input
                          type={showPassword ? "text" : "password"}
                          autoComplete="new-password"
                          className="pr-10"
                          {...field}
                        />
                      </FormControl>
                      <button
                        type="button"
                        onClick={() => setShowPassword((value) => !value)}
                        aria-label={showPassword ? "Hide password" : "Show password"}
                        className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-muted-foreground hover:text-foreground"
                      >
                        {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                      </button>
                    </div>
                    <ul className="space-y-1">
                      {PASSWORD_REQUIREMENTS.map((requirement) => {
                        const isSatisfied = requirement.isSatisfied(password);
                        return (
                          <li
                            key={requirement.label}
                            className={cn(
                              "flex items-center gap-1.5 text-caption",
                              isSatisfied ? "text-emerald-600" : "text-muted-foreground"
                            )}
                          >
                            {isSatisfied ? (
                              <Check className="h-3.5 w-3.5" aria-hidden />
                            ) : (
                              <X className="h-3.5 w-3.5" aria-hidden />
                            )}
                            {requirement.label}
                          </li>
                        );
                      })}
                    </ul>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>Confirm password</FormLabel>
                    <div className="relative">
                      <FormControl>
                        <Input
                          type={showConfirmPassword ? "text" : "password"}
                          autoComplete="new-password"
                          className="pr-10"
                          {...field}
                        />
                      </FormControl>
                      <button
                        type="button"
                        onClick={() => setShowConfirmPassword((value) => !value)}
                        aria-label={showConfirmPassword ? "Hide password" : "Show password"}
                        className="absolute inset-y-0 right-0 flex w-10 items-center justify-center text-muted-foreground hover:text-foreground"
                      >
                        {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                      </button>
                    </div>
                    <FormMessage />
                  </FormItem>
                )}
              />
              {formError && <p className="text-small text-destructive">{formError}</p>}
              <Button type="submit" className="w-full" disabled={form.formState.isSubmitting}>
                {form.formState.isSubmitting ? "Creating account…" : "Create account"}
              </Button>
            </form>
          </Form>
        </CardContent>
      </Card>

      <p className="text-center text-small text-muted-foreground">
        Already have an account?{" "}
        <Link to="/login" className="font-medium text-primary underline-offset-2 hover:underline">
          Log in
        </Link>
      </p>
    </div>
  );
}
