export default function PlaceholderPage({ name }: { name: string }) {
    return (
        <div className="container section">
            <h1>{name}</h1>
            <p>This page is under construction.</p>
        </div>
    );
}
