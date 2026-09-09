// Keep in lockstep with AcikIstihbarat.API/Helpers/NewsletterDisplayNames.cs
// Known drift risk - tracked in ProjectImplementationDocs/TODO-AcikIstihbarat.md
// ("Unify newsletter display name source of truth").
export const NEWSLETTER_LABELS: Record<string, string> = {
  AcikGazete: 'Gazete Özetleri',
  AcikKose: 'Köşe Yazarları',
};

export function resolveNewsletterLabel(folder: string): string {
  return NEWSLETTER_LABELS[folder] ?? folder;
}
