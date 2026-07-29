import { z } from 'zod';

const MAX_TRIP_NAME_LENGTH = 200;

export const createTripSchema = z.object({
  name: z.string().min(1, 'Trip name is required').max(MAX_TRIP_NAME_LENGTH, 'Trip name is too long'),
});
export type CreateTripFormValues = z.infer<typeof createTripSchema>;

export const setDatesSchema = z
  .object({
    startDate: z.string().min(1, 'Start date is required'),
    endDate: z.string().min(1, 'End date is required'),
  })
  .refine((values) => values.startDate <= values.endDate, {
    message: 'Start date must be on or before the end date',
    path: ['endDate'],
  });
export type SetDatesFormValues = z.infer<typeof setDatesSchema>;

export const addToTripSchema = z.object({
  tripId: z.string().min(1, 'Choose a trip'),
  itineraryDayId: z.string().min(1, 'Choose a day'),
});
export type AddToTripFormValues = z.infer<typeof addToTripSchema>;
