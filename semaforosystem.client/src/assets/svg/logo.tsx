import type { SVGAttributes } from 'react'

const LogoSvg = (props: SVGAttributes<SVGElement>) => {
  return (
    <svg
      width='28'
      height='28'
      viewBox='0 0 28 28'
      fill='none'
      xmlns='http://www.w3.org/2000/svg'
      {...props}
    >
      <rect width='28' height='28' rx='6' fill='var(--primary)' />
      <text
        x='50%'
        y='55%'
        dominantBaseline='middle'
        textAnchor='middle'
        fill='var(--primary-foreground)'
        fontSize='16'
        fontWeight='700'
        fontFamily='system-ui, sans-serif'
      >
        S
      </text>
    </svg>
  )
}

export default LogoSvg
