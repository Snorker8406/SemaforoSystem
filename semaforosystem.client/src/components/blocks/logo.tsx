import LogoSvg from '@/assets/svg/logo'

interface LogoProps {
  className?: string
}

const Logo = ({ className }: LogoProps) => {
  return (
    <div className={className}>
      <LogoSvg className='size-7' />
    </div>
  )
}

export default Logo
