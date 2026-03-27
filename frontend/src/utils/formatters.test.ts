import { describe, it, expect } from 'vitest'
import { formatBytes, formatDate, formatDateTime } from './formatters'

describe('formatBytes', () => {
  it('returns "0 B" for 0 bytes', () => {
    expect(formatBytes(0)).toBe('0 B')
  })

  it('returns bytes string for values under 1 KB', () => {
    expect(formatBytes(1023)).toBe('1023 B')
  })

  it('returns KB string for 1024 bytes', () => {
    // parseFloat strips trailing zero: parseFloat('1.0') === 1 → '1 KB'
    expect(formatBytes(1024)).toBe('1 KB')
  })

  it('returns MB string for 1048576 bytes', () => {
    expect(formatBytes(1048576)).toBe('1 MB')
  })

  it('returns GB string for 1 GB', () => {
    expect(formatBytes(1024 * 1024 * 1024)).toBe('1 GB')
  })

  it('returns decimal for non-round values', () => {
    expect(formatBytes(1536)).toBe('1.5 KB')
  })
})

describe('formatDate', () => {
  it('formats a valid ISO date string to a locale string', () => {
    const result = formatDate('2024-03-15T00:00:00.000Z')
    // The result should contain the year 2024
    expect(result).toContain('2024')
    expect(result).toMatch(/Mar|March|3/)
  })

  it('formats a Date object correctly', () => {
    // Use noon UTC to avoid midnight timezone crossover (UTC-X would show Dec 31)
    const date = new Date('2024-07-15T12:00:00.000Z')
    const result = formatDate(date)
    expect(result).toContain('2024')
    expect(result).toMatch(/Jul|July|7/)
  })

  it('does not include time information', () => {
    const result = formatDate('2024-06-15T12:30:00.000Z')
    // Should not contain AM/PM or colon for time
    expect(result).not.toMatch(/\d{1,2}:\d{2}/)
  })
})

describe('formatDateTime', () => {
  it('formats a valid ISO date-time string with time portion', () => {
    const result = formatDateTime('2024-03-15T10:30:00.000Z')
    expect(result).toContain('2024')
    // Should include a time component with AM/PM
    expect(result).toMatch(/AM|PM/)
  })

  it('formats a Date object correctly', () => {
    const date = new Date('2024-01-01T14:00:00.000Z')
    const result = formatDateTime(date)
    expect(result).toContain('2024')
    expect(result).toMatch(/AM|PM/)
  })

  it('includes both date and time information', () => {
    const result = formatDateTime('2024-06-15T12:30:00.000Z')
    // Should contain year and a time separator
    expect(result).toContain('2024')
    expect(result).toMatch(/\d{1,2}:\d{2}/)
  })
})
