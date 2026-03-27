import { describe, it, expect } from 'vitest'
import { cn } from './cn'

describe('cn', () => {
  it('merges multiple class names into one string', () => {
    expect(cn('foo', 'bar')).toBe('foo bar')
  })

  it('returns a single class name unchanged', () => {
    expect(cn('foo')).toBe('foo')
  })

  it('handles empty string input', () => {
    expect(cn('')).toBe('')
  })

  it('ignores falsy values (undefined, null, false)', () => {
    expect(cn('foo', undefined, 'bar')).toBe('foo bar')
    expect(cn('foo', null, 'bar')).toBe('foo bar')
    expect(cn('foo', false, 'bar')).toBe('foo bar')
  })

  it('handles conditional classes with object syntax', () => {
    expect(cn('base', { active: true, disabled: false })).toBe('base active')
  })

  it('handles array of class names', () => {
    expect(cn(['foo', 'bar'])).toBe('foo bar')
  })

  it('handles mixed inputs', () => {
    const isActive = true
    const isDisabled = false
    const result = cn('base', isActive && 'active', isDisabled && 'disabled')
    expect(result).toBe('base active')
  })

  it('returns empty string when no args passed', () => {
    expect(cn()).toBe('')
  })
})
