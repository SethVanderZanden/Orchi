import { useId } from 'react'

type OrchiAiIconProps = {
  className?: string
}

const GRADIENT_STOPS = [
  { offset: '0%', color: '#f9a8d4' },
  { offset: '30%', color: '#c084fc' },
  { offset: '60%', color: '#60a5fa' },
  { offset: '100%', color: '#f87171' }
] as const

export function OrchiAiIcon({ className }: OrchiAiIconProps): React.JSX.Element {
  const gradientId = useId().replace(/:/g, '')

  return (
    <svg
      viewBox="4.5 4.5 15 16"
      fill="none"
      xmlns="http://www.w3.org/2000/svg"
      aria-hidden="true"
      className={className ? `size-6 shrink-0 ${className}` : 'size-6 shrink-0'}
    >
      <defs>
        <linearGradient
          id={gradientId}
          x1="5"
          y1="5"
          x2="19"
          y2="20"
          gradientUnits="userSpaceOnUse"
        >
          {GRADIENT_STOPS.map((stop) => (
            <stop key={stop.offset} offset={stop.offset} stopColor={stop.color} />
          ))}
        </linearGradient>
      </defs>
      <path
        d="M6.5 5.5c-.8 1.6-1.2 3.2-.6 4.8M17.5 5.5c.8 1.6 1.2 3.2.6 4.8"
        stroke={`url(#${gradientId})`}
        strokeWidth="1.85"
        strokeLinecap="round"
      />
      <circle cx="12" cy="13" r="6.5" stroke={`url(#${gradientId})`} strokeWidth="1.85" />
      <ellipse
        cx="12"
        cy="15.2"
        rx="2.6"
        ry="1.8"
        stroke={`url(#${gradientId})`}
        strokeWidth="1.85"
      />
      <circle cx="10.4" cy="15.1" r="0.55" fill={`url(#${gradientId})`} />
      <circle cx="13.6" cy="15.1" r="0.55" fill={`url(#${gradientId})`} />
      <circle cx="9.4" cy="11.2" r="0.9" fill={`url(#${gradientId})`} />
      <circle cx="14.6" cy="11.2" r="0.9" fill={`url(#${gradientId})`} />
    </svg>
  )
}
