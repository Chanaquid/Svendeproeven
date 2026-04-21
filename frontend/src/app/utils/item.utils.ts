import { ItemAvailability, ItemCondition } from '../dtos/enums';

export function getConditionClass(condition: ItemCondition): string {
  switch (condition) {
    case ItemCondition.Excellent:
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    case ItemCondition.Good:
      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    case ItemCondition.Fair:
      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
    case ItemCondition.Poor:
      return 'bg-rose-500/10 text-rose-400 border-rose-500/20';
    default:
      return 'bg-zinc-800 text-zinc-400 border-zinc-700';
  }
}

export function getAvailabilityLabel(availability: ItemAvailability): string {
  switch (availability) {
    case ItemAvailability.Available:
      return 'Available';
    case ItemAvailability.OnRent:
      return 'On rent';
    case ItemAvailability.Unavailable:
      return 'Unavailable';
    default:
      return 'Unknown';
  }
}

export function getAvailabilityClass(availability: ItemAvailability): string {
  switch (availability) {
    case ItemAvailability.Available:
      return 'available';
    case ItemAvailability.OnRent:
      return 'rent';
    case ItemAvailability.Unavailable:
      return 'unavailable';
    default:
      return '';
  }
}

export function getCategoryEmoji(categoryName: string): string {
  const map: Record<string, string> = {
    electronics: '📱',
    tools: '🔧',
    sports: '⚽',
    music: '🎸',
    books: '📚',
    camping: '⛺',
    photography: '📷',
    cameras: '📷',
    gaming: '🎮',
    gardening: '🌱',
    garden: '🪴',
    biking: '🚲',
    bikes: '🚲',
    kitchen: '🍳',
    cleaning: '🧹',
    fashion: '👗',
    art: '🎨',
    baby: '👶',
    events: '🎉',
    auto: '🚗',
    other: '📦',
    others: '📦',
  };
  return map[categoryName.toLowerCase()] ?? '📦';
}

export function getOwnerInitials(fullName: string): string {
  return fullName
    .split(' ')
    .map((p) => p[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}
