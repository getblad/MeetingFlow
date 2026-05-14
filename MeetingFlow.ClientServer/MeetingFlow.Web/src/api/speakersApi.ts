import { get } from './http';
import type { Speaker } from '../types/models';

export function fetchSpeaker(id: string): Promise<Speaker> {
  return get<Speaker>(`/speakers/${id}`);
}
