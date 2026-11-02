import { useEffect, useSyncExternalStore } from 'react';
import type { StationController, StationState } from './StationController';

const TICK_INTERVAL_MS = 20_000;

/** Denetleyicinin durumuna abone olur; ilk eşitlemeyi ve düzenli kalp atışını yürütür. */
export function useStation(controller: StationController): StationState {
  const state = useSyncExternalStore(controller.subscribe, controller.getState);

  useEffect(() => {
    controller.synchronize();
    const timer = setInterval(() => controller.tick(), TICK_INTERVAL_MS);
    return () => clearInterval(timer);
  }, [controller]);

  return state;
}
