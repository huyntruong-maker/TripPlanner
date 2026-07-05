import { useState } from 'react';

interface PhotoCarouselProps {
  photos: string[];
  destinationName: string;
}

/** F2/US2 — photo carousel with a placeholder when the destination has no photos. */
export function PhotoCarousel({ photos, destinationName }: PhotoCarouselProps) {
  const [index, setIndex] = useState(0);
  const total = photos.length;

  if (total === 0) {
    return (
      <div
        className="w-full aspect-[16/9] rounded-xl elevation-l1 bg-surface-container-high flex items-center justify-center text-on-surface-variant text-body-md"
        role="img"
        aria-label="No photos available"
      >
        No photos available
      </div>
    );
  }

  function showPrevious() {
    setIndex((current) => (current - 1 + total) % total);
  }

  function showNext() {
    setIndex((current) => (current + 1) % total);
  }

  return (
    <div className="relative rounded-xl overflow-hidden elevation-l1">
      <img
        className="photo-carousel-image w-full aspect-[16/9] object-cover"
        src={photos[index]}
        alt={`${destinationName} photo ${index + 1} of ${total}`}
      />
      {total > 1 && (
        <>
          <button
            type="button"
            onClick={showPrevious}
            aria-label="Previous photo"
            className="absolute left-4 top-1/2 -translate-y-1/2 bg-white/80 hover:bg-white rounded-full p-2 elevation-l1 transition-colors"
          >
            <span className="material-symbols-outlined" aria-hidden="true">
              chevron_left
            </span>
          </button>
          <button
            type="button"
            onClick={showNext}
            aria-label="Next photo"
            className="absolute right-4 top-1/2 -translate-y-1/2 bg-white/80 hover:bg-white rounded-full p-2 elevation-l1 transition-colors"
          >
            <span className="material-symbols-outlined" aria-hidden="true">
              chevron_right
            </span>
          </button>
          <span className="absolute bottom-3 right-3 bg-black/50 text-white text-label-sm rounded-full px-2 py-0.5">
            {index + 1} / {total}
          </span>
        </>
      )}
    </div>
  );
}
