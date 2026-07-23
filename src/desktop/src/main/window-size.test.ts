import { describe, expect, it } from 'vitest'

import {
  DEFAULT_WINDOW_MAX_HEIGHT,
  DEFAULT_WINDOW_MAX_WIDTH,
  DEFAULT_WINDOW_MIN_HEIGHT,
  DEFAULT_WINDOW_MIN_WIDTH,
  getDefaultWindowSize
} from './window-size'

describe('getDefaultWindowSize', () => {
  it('uses a larger floor than the old 900×670 defaults on typical laptop displays', () => {
    expect(getDefaultWindowSize({ width: 1440, height: 900 })).toEqual({
      width: DEFAULT_WINDOW_MIN_WIDTH,
      height: DEFAULT_WINDOW_MIN_HEIGHT
    })
  })

  it('scales with the work area on common desktop resolutions', () => {
    expect(getDefaultWindowSize({ width: 1920, height: 1080 })).toEqual({
      width: Math.round(1920 * 0.78),
      height: Math.round(1080 * 0.82)
    })
  })

  it('caps size on large / ultra-wide displays', () => {
    expect(getDefaultWindowSize({ width: 3440, height: 1440 })).toEqual({
      width: DEFAULT_WINDOW_MAX_WIDTH,
      height: DEFAULT_WINDOW_MAX_HEIGHT
    })
  })

  it('never exceeds a small work area', () => {
    expect(getDefaultWindowSize({ width: 1024, height: 700 })).toEqual({
      width: 1024,
      height: 700
    })
  })
})
