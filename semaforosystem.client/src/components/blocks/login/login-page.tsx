'use client'

import AuthBackgroundShape from '@/assets/svg/auth-background-shape'
import Logo from '@/components/blocks/logo'
import LoginForm from '@/components/blocks/login/login-form'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'

const LoginPage = () => {
  return (
    <div className='relative flex min-h-screen items-center justify-center overflow-hidden bg-background px-4 py-12'>
      {/* Background decoration */}
      <AuthBackgroundShape className='pointer-events-none absolute -right-40 -top-40 opacity-50 sm:opacity-100' />
      <AuthBackgroundShape className='pointer-events-none absolute -bottom-40 -left-40 rotate-180 opacity-50 sm:opacity-100' />

      <Card className='z-10 w-full max-w-md shadow-lg'>
        <CardHeader className='space-y-3 text-center'>
          <div className='flex justify-center'>
            <Logo />
          </div>
          <CardTitle className='text-2xl font-bold'>
            Bienvenido de nuevo
          </CardTitle>
          <CardDescription>
            Inicia sesión en tu cuenta para continuar
          </CardDescription>
        </CardHeader>

        <CardContent className='space-y-6'>
          {/* Login form */}
          <LoginForm />

          {/* Sign up link */}
          <p className='text-center text-sm text-muted-foreground'>
            ¿No tienes una cuenta?{' '}
            <a
              href='#'
              className='font-medium text-primary underline-offset-4 hover:underline'
            >
              Regístrate
            </a>
          </p>
        </CardContent>
      </Card>
    </div>
  )
}

export default LoginPage
