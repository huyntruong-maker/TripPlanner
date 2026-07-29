import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { getApiErrorMessage } from '../../../api/errors';
import { setTripDates } from '../../../api/trips';
import { useToast } from '../../../components/toast/ToastProvider';
import type { Trip } from '../../../types';
import { tripQueryKey } from '../hooks/useTrip';
import { setDatesSchema, type SetDatesFormValues } from '../lib/schemas';

const INPUT_CLASSES =
  'w-full h-12 px-4 rounded-lg border border-outline-variant focus:ring-2 focus:ring-primary/20 focus:border-primary transition-all text-body-md outline-none';

interface SetDatesFormProps {
  trip: Trip;
  onSaved: (warningErrorCode: string | null) => void;
}

export function SetDatesForm({ trip, onSaved }: SetDatesFormProps) {
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [formError, setFormError] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SetDatesFormValues>({
    resolver: zodResolver(setDatesSchema),
    defaultValues: { startDate: trip.startDate ?? '', endDate: trip.endDate ?? '' },
  });

  async function onSubmit(values: SetDatesFormValues) {
    setFormError(null);
    try {
      const { warningErrorCode } = await setTripDates(trip.id, values);
      await queryClient.invalidateQueries({ queryKey: tripQueryKey(trip.id) });
      onSaved(warningErrorCode);
    } catch (err) {
      const message = getApiErrorMessage(err, 'Could not save the trip dates.');
      setFormError(message);
      showToast(message);
    }
  }

  return (
    <form
      onSubmit={handleSubmit(onSubmit)}
      noValidate
      className="flex flex-col md:flex-row md:items-end gap-stack-lg"
    >
      <div className="w-full md:w-1/3 space-y-2">
        <label
          htmlFor="set-dates-startDate"
          className="block text-label-md font-label-md text-on-surface-variant"
        >
          Start date
        </label>
        <input
          id="set-dates-startDate"
          type="date"
          className={INPUT_CLASSES}
          {...register('startDate')}
          aria-invalid={Boolean(errors.startDate)}
          aria-describedby={errors.startDate ? 'set-dates-startDate-error' : undefined}
        />
        {errors.startDate && (
          <p className="text-error text-label-sm font-semibold" id="set-dates-startDate-error">
            {errors.startDate.message}
          </p>
        )}
      </div>
      <div className="w-full md:w-1/3 space-y-2">
        <label
          htmlFor="set-dates-endDate"
          className="block text-label-md font-label-md text-on-surface-variant"
        >
          End date
        </label>
        <input
          id="set-dates-endDate"
          type="date"
          className={INPUT_CLASSES}
          {...register('endDate')}
          aria-invalid={Boolean(errors.endDate)}
          aria-describedby={errors.endDate ? 'set-dates-endDate-error' : undefined}
        />
        {errors.endDate && (
          <p className="text-error text-label-sm font-semibold" id="set-dates-endDate-error">
            {errors.endDate.message}
          </p>
        )}
      </div>
      {formError && (
        <p className="text-error text-label-sm font-semibold w-full" role="alert">
          {formError}
        </p>
      )}
      <div className="w-full md:w-auto">
        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full md:w-auto bg-primary text-on-primary h-12 px-8 rounded-lg font-label-md hover:opacity-90 active:scale-95 transition-all disabled:opacity-60 disabled:cursor-not-allowed flex items-center justify-center gap-2"
        >
          <span className="material-symbols-outlined text-[20px]" aria-hidden="true">
            {trip.startDate ? 'sync' : 'event_available'}
          </span>
          {isSubmitting ? 'Saving…' : trip.startDate ? 'Update dates' : 'Set dates'}
        </button>
      </div>
    </form>
  );
}