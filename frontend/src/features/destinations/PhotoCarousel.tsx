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
      <div className="photo-carousel photo-carousel--placeholder" role="img" aria-label="No photos available">
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
    <div className="photo-carousel">
      <img
        className="photo-carousel-image"
        src={photos[index]}
        alt={`${destinationName} photo ${index + 1} of ${total}`}
      />
      {total > 1 && (
        <div className="photo-carousel-controls">
          <button type="button" onClick={showPrevious} aria-label="Previous photo">
            ‹
          </button>
          <span className="photo-carousel-counter">
            {index + 1} / {total}
          </span>
          <button type="button" onClick={showNext} aria-label="Next photo">
            ›
          </button>
        </div>
      )}
    </div>
  );
}
