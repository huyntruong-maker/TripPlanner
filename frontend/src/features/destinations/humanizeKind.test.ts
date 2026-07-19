import { describe, expect, it } from 'vitest';
import { humanizeKind, kindKey } from './humanizeKind';

describe('humanizeKind', () => {
  it('replaces underscores with spaces and sentence-cases the result', () => {
    expect(humanizeKind('other_buildings_and_structures')).toBe('Other buildings and structures');
  });

  it('sentence-cases an all-caps provider label', () => {
    expect(humanizeKind('SKYSCRAPERS')).toBe('Skyscrapers');
  });

  it('handles a single-word label', () => {
    expect(humanizeKind('interesting_places')).toBe('Interesting places');
  });

  it('is idempotent regardless of the source casing', () => {
    expect(humanizeKind('Art_galleries')).toBe('Art galleries');
    expect(humanizeKind('art_galleries')).toBe('Art galleries');
  });

  it('returns an empty string unchanged', () => {
    expect(humanizeKind('')).toBe('');
  });
});

describe('kindKey', () => {
  it('lowercases for a case-insensitive dedup key', () => {
    expect(kindKey('Art_galleries')).toBe('art_galleries');
    expect(kindKey('art_galleries')).toBe('art_galleries');
  });

  it('keeps genuinely distinct values distinct', () => {
    expect(kindKey('Bank')).not.toBe(kindKey('Banks'));
  });
});
