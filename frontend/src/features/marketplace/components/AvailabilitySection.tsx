import { useState } from 'react';
import { useMarketplace } from '../useMarketplace';
import type { AvailabilitySlotRequest } from '../../../types/marketplace';

interface AvailabilitySlot {
  dayOfWeek: number;
  startTime: string;
  endTime: string;
}

const DAY_LABELS = [
  'Domingo',
  'Lunes',
  'Martes',
  'Miércoles',
  'Jueves',
  'Viernes',
  'Sábado',
];

export function AvailabilitySection() {
  const { state, replaceAvailability } = useMarketplace();
  const [slots, setSlots] = useState<AvailabilitySlot[]>([
    { dayOfWeek: 1, startTime: '09:00', endTime: '18:00' },
  ]);

  const updateDay = (index: number, dayOfWeek: number) => {
    setSlots((current) =>
      current.map((slot, slotIndex) =>
        slotIndex === index ? { ...slot, dayOfWeek } : slot
      )
    );
  };

  const updateStart = (index: number, startTime: string) => {
    setSlots((current) =>
      current.map((slot, slotIndex) =>
        slotIndex === index ? { ...slot, startTime } : slot
      )
    );
  };

  const updateEnd = (index: number, endTime: string) => {
    setSlots((current) =>
      current.map((slot, slotIndex) =>
        slotIndex === index ? { ...slot, endTime } : slot
      )
    );
  };

  const removeSlot = (index: number) => {
    setSlots((current) =>
      current.filter((_, slotIndex) => slotIndex !== index)
    );
  };

  const addSlot = () => {
    setSlots((current) => [
      ...current,
      { dayOfWeek: 1, startTime: '09:00', endTime: '18:00' },
    ]);
  };

  const save = async () => {
    const payload: AvailabilitySlotRequest[] = slots.map((slot) => ({
      dayOfWeek: slot.dayOfWeek,
      startTime: slot.startTime,
      endTime: slot.endTime,
    }));

    await replaceAvailability(payload);
  };

  return (
    <section className="worker-panel">
      <h2>Disponibilidad</h2>
      <ul className="worker-offerings">
        {slots.map((slot, index) => (
          <li key={index}>
            <label>
              Día
              <select
                value={slot.dayOfWeek}
                onChange={(event) =>
                  updateDay(index, Number(event.target.value))
                }
              >
                {DAY_LABELS.map((label, day) => (
                  <option key={day} value={day}>
                    {label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Inicio
              <input
                type="time"
                value={slot.startTime}
                onChange={(event) => updateStart(index, event.target.value)}
              />
            </label>
            <label>
              Fin
              <input
                type="time"
                value={slot.endTime}
                onChange={(event) => updateEnd(index, event.target.value)}
              />
            </label>
            <button type="button" onClick={() => removeSlot(index)}>
              Eliminar
            </button>
          </li>
        ))}
      </ul>
      <button type="button" onClick={addSlot}>
        Agregar horario
      </button>
      <button type="button" onClick={() => void save()}>
        Guardar disponibilidad
      </button>
      {state.kind === 'error' ? (
        <p role="alert">{state.message}</p>
      ) : null}
    </section>
  );
}
