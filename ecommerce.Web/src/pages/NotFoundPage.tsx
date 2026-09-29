import { ButtonLink } from '../components/ui/button'

export default function NotFoundPage() {
  return (
    <section className="py-16 text-center">
      <p className="m-0 font-mono text-[13px] text-muted">404</p>
      <h1 className="mt-2 mb-0">That page isn’t here</h1>
      <p className="mx-auto mt-2 max-w-sm text-sm text-muted">
        The link may be out of date, or the item was removed from the catalogue.
      </p>
      <div className="mt-6 flex justify-center">
        <ButtonLink to="/products">Browse products</ButtonLink>
      </div>
    </section>
  )
}
