import {
  FacebookIcon,
  InstagramIcon,
  LinkedinIcon,
  TwitterIcon,
} from 'lucide-react'

const AppFooter = () => {
  return (
    <footer>
      <div className='text-muted-foreground mx-auto flex size-full max-w-7xl items-center justify-between gap-3 px-4 py-3 max-sm:flex-col sm:gap-6 sm:px-6'>
        <p className='text-sm text-balance max-sm:text-center'>
          {`©${new Date().getFullYear()}`}{' '}
          <a href='#' className='text-primary'>
            SemaforoSystem
          </a>
          , Made for better web design
        </p>
        <div className='flex items-center gap-5'>
          <a href='#'>
            <FacebookIcon className='size-4' />
          </a>
          <a href='#'>
            <InstagramIcon className='size-4' />
          </a>
          <a href='#'>
            <LinkedinIcon className='size-4' />
          </a>
          <a href='#'>
            <TwitterIcon className='size-4' />
          </a>
        </div>
      </div>
    </footer>
  )
}

export default AppFooter
