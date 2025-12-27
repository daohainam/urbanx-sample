import { useState, useEffect } from 'react';
import { Plus, Edit, Trash2, Save, X } from 'lucide-react';
import { useAuth } from 'react-oidc-context';
import { ApiClient } from '../lib/api';
import type { Category } from '../types';

export default function CategoriesPage() {
  const auth = useAuth();
  const [categories, setCategories] = useState<Category[]>([]);
  const [loading, setLoading] = useState(true);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [showAddForm, setShowAddForm] = useState(false);
  const [formData, setFormData] = useState({ name: '', description: '' });

  const api = new ApiClient(() => auth.user?.access_token);
  const merchantId = 'c0a80121-0000-0000-0000-000000000001'; // TODO: Get from user profile

  useEffect(() => {
    loadCategories();
  }, []);

  const loadCategories = async () => {
    try {
      setLoading(true);
      const data = await api.get<Category[]>(`/api/merchants/${merchantId}/categories`);
      setCategories(data);
    } catch (error) {
      console.error('Failed to load categories:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleAdd = async () => {
    if (!formData.name.trim()) return;

    try {
      await api.post(`/api/merchants/${merchantId}/categories`, {
        name: formData.name,
        description: formData.description,
      });
      setFormData({ name: '', description: '' });
      setShowAddForm(false);
      loadCategories();
    } catch (error) {
      console.error('Failed to add category:', error);
      alert('Failed to add category');
    }
  };

  const handleUpdate = async (category: Category) => {
    try {
      await api.put(
        `/api/merchants/${merchantId}/categories/${category.id}`,
        category
      );
      setEditingId(null);
      loadCategories();
    } catch (error) {
      console.error('Failed to update category:', error);
      alert('Failed to update category');
    }
  };

  const handleDelete = async (id: string) => {
    if (!confirm('Are you sure you want to delete this category?')) return;

    try {
      await api.delete(`/api/merchants/${merchantId}/categories/${id}`);
      loadCategories();
    } catch (error) {
      console.error('Failed to delete category:', error);
      alert('Failed to delete category');
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="spinner"></div>
      </div>
    );
  }

  return (
    <div className="animate-fade-in">
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold text-neutral-900 mb-2">Categories</h1>
          <p className="text-neutral-600">Manage your product categories</p>
        </div>
        <button
          onClick={() => setShowAddForm(true)}
          className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
        >
          <Plus className="w-5 h-5" />
          Add Category
        </button>
      </div>

      {showAddForm && (
        <div className="bg-white rounded-lg shadow-soft p-6 border border-neutral-200 mb-6">
          <h3 className="text-lg font-semibold mb-4">Add New Category</h3>
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Name *
              </label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                placeholder="Enter category name"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-neutral-700 mb-1">
                Description
              </label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                className="w-full px-3 py-2 border border-neutral-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                rows={3}
                placeholder="Enter category description"
              />
            </div>
            <div className="flex gap-2">
              <button
                onClick={handleAdd}
                className="flex items-center gap-2 px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors"
              >
                <Save className="w-4 h-4" />
                Save
              </button>
              <button
                onClick={() => {
                  setShowAddForm(false);
                  setFormData({ name: '', description: '' });
                }}
                className="flex items-center gap-2 px-4 py-2 bg-neutral-200 text-neutral-700 rounded-lg hover:bg-neutral-300 transition-colors"
              >
                <X className="w-4 h-4" />
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="bg-white rounded-lg shadow-soft border border-neutral-200">
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead className="bg-neutral-50 border-b border-neutral-200">
              <tr>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Name
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Description
                </th>
                <th className="px-6 py-3 text-left text-sm font-semibold text-neutral-900">
                  Status
                </th>
                <th className="px-6 py-3 text-right text-sm font-semibold text-neutral-900">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-200">
              {categories.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-6 py-12 text-center text-neutral-500">
                    No categories found. Add your first category to get started.
                  </td>
                </tr>
              ) : (
                categories.map((category) => (
                  <tr key={category.id} className="hover:bg-neutral-50">
                    <td className="px-6 py-4">
                      {editingId === category.id ? (
                        <input
                          type="text"
                          value={category.name}
                          onChange={(e) =>
                            setCategories(
                              categories.map((c) =>
                                c.id === category.id ? { ...c, name: e.target.value } : c
                              )
                            )
                          }
                          className="w-full px-2 py-1 border border-neutral-300 rounded"
                        />
                      ) : (
                        <span className="text-sm font-medium text-neutral-900">
                          {category.name}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      {editingId === category.id ? (
                        <input
                          type="text"
                          value={category.description || ''}
                          onChange={(e) =>
                            setCategories(
                              categories.map((c) =>
                                c.id === category.id
                                  ? { ...c, description: e.target.value }
                                  : c
                              )
                            )
                          }
                          className="w-full px-2 py-1 border border-neutral-300 rounded"
                        />
                      ) : (
                        <span className="text-sm text-neutral-600">
                          {category.description || '-'}
                        </span>
                      )}
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex px-2 py-1 text-xs font-semibold rounded-full ${
                          category.isActive
                            ? 'bg-green-100 text-green-800'
                            : 'bg-neutral-100 text-neutral-800'
                        }`}
                      >
                        {category.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        {editingId === category.id ? (
                          <>
                            <button
                              onClick={() => handleUpdate(category)}
                              className="p-2 text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                              title="Save"
                            >
                              <Save className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => {
                                setEditingId(null);
                                loadCategories();
                              }}
                              className="p-2 text-neutral-600 hover:bg-neutral-100 rounded-lg transition-colors"
                              title="Cancel"
                            >
                              <X className="w-4 h-4" />
                            </button>
                          </>
                        ) : (
                          <>
                            <button
                              onClick={() => setEditingId(category.id)}
                              className="p-2 text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                              title="Edit"
                            >
                              <Edit className="w-4 h-4" />
                            </button>
                            <button
                              onClick={() => handleDelete(category.id)}
                              className="p-2 text-accent-600 hover:bg-accent-50 rounded-lg transition-colors"
                              title="Delete"
                            >
                              <Trash2 className="w-4 h-4" />
                            </button>
                          </>
                        )}
                      </div>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
