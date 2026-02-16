'use client'

import { useState } from 'react'
import { Eye, EyeOff, Loader2 } from 'lucide-react'

import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

const LoginForm = () => {
  const [showPassword, setShowPassword] = useState(false)
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    setIsLoading(true)
    // Simulate login
    setTimeout(() => setIsLoading(false), 2000)
  }

  return (
    <form onSubmit={handleSubmit} className='space-y-4'>
      <div className='space-y-2'>
        <Label htmlFor='email'>Email</Label>
        <Input
          id='email'
          type='email'
          placeholder='correo@ejemplo.com'
          required
          autoComplete='email'
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

      <Button type='submit' className='w-full' disabled={isLoading}>
        {isLoading ? (
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
