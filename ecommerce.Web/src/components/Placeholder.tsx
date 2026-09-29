export function Placeholder({ title }: { title: string }) {
  return (
    <section>
      <h1 className="m-0">{title}</h1>
      <p className="mt-2 text-sm text-muted">Not built yet.</p>
    </section>
  )
}
