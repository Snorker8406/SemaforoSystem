'use client'

import { useState } from 'react'
import { Eye, EyeOff, Loader2 } from 'lucide-react'
import { useMutation } from '@tanstack/react-query'
import { useRouter } from '@tanstack/react-router'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { login, type LoginRequest } from '@/services/auth-service'
import { ApiError } from '@/lib/api-client'

const LoginForm = () => {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const router = useRouter()

  const loginMutation = useMutation({
    mutationFn: (credentials: LoginRequest) => login(credentials),
    onSuccess: () => {
      // La cookie de sesión ya fue seteada por el servidor
      router.navigate({ to: '/' })
    },
    onError: (err) => {
      if (err instanceof ApiError) {
        if (err.status === 401) {
          setError('Credenciales inválidas. Verifica tu email y contraseña.')
        } else {
          setError(`Error del servidor (${err.status}). Intenta de nuevo.`)
        }
      } else {
        setError('No se pudo conectar al servidor. Verifica tu conexión.')
      }
    },
  })

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)
    loginMutation.mutate({ email, password })
  }

  return (
    <form onSubmit={handleSubmit} className='space-y-4'>
      {error && (
        <div className='rounded-md bg-destructive/15 p-3 text-sm text-destructive'>
          {error}
        </div>
      )}

      <div className='space-y-2'>
        <Label htmlFor='email'>Email</Label>
        <Input
          id='email'
          type='email'
          placeholder='correo@ejemplo.com'
          required
          autoComplete='email'
          value={email}
          onChange={(e) => setEmail(e.target.value)}
        />
      </div>

      <div className='space-y-2'>
        <div className='flex items-center justify-between'>
          <Label htmlFor='password'>Contraseña</Label>
          <a
            href='#'
            className='text-sm font-medium text-primary underline-offset-4 hover:underline'
          >
            ¿Olvidaste tu contraseña?
          </a>
        </div>
        <div className='relative'>
          <Input
            id='password'
            type={showPassword ? 'text' : 'password'}
            placeholder='••••••••'
            required
            autoComplete='current-password'
            className='pr-10'
            value={password}
            onChange={(e) => setPassword(e.target.value)}
          />
          <Button
            type='button'
            variant='ghost'
            size='icon'
            className='absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent'
            onClick={() => setShowPassword(!showPassword)}
            aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
          >
            {showPassword ? (
              <EyeOff className='size-4 text-muted-foreground' />
            ) : (
              <Eye className='size-4 text-muted-foreground' />
            )}
          </Button>
        </div>
      </div>

      <div className='flex items-center space-x-2'>
        <Checkbox id='remember' />
        <Label htmlFor='remember' className='text-sm font-normal'>
          Recordarme
        </Label>
      </div>

      <Button type='submit' className='w-full' disabled={loginMutation.isPending}>
        {loginMutation.isPending ? (
          <>
            <Loader2 className='mr-2 size-4 animate-spin' />
            Iniciando sesión...
          </>
        ) : (
          'Iniciar sesión'
        )}
      </Button>
    </form>
  )
}

export default LoginForm
